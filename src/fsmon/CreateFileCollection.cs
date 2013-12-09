using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;

namespace Fws.Collections
{
	/// <summary>
	/// Detects the arrival of files in a directory and makes them available to a client class
	/// as an IEnumerable of fully pathed file names. Unlike the .NET FileSystemWatcher, this
	/// class yields files that exist when the object is constructed. Also, it is not an IDisposable.
	/// </summary>
	/// <remarks>
	/// <para>
	/// If a file arrives during the execution of this class's constructor, it may be reported more than
	/// once. Also, some programs write their files in such a way that the underlying FileSystemWatcher
	/// will fire a Create event more than once. In those cases, this class will yield the
	/// file multiple times.
	/// </para><para>
	/// Client code must account for these possibilities. It is envisioned that wrapping classes may
	/// refine the yielded files by waiting for them to quiesce, filtering out duplicates, etc.
	/// </para>
	/// <para>
	/// This class is thread-safe: more than one thread may enumerate the files presented by a
	/// single instance of this class, and each thread will get all the files.
	/// </para>
	/// </remarks>
	public sealed class CreatedFileCollection : IEnumerable<string>
	{
		#region Fields
		readonly string _directory;
		readonly string _filePattern;
		readonly CancellationToken _cancellationToken;
		#endregion

		#region Nested Class to Collect Results
		/// <summary>
		/// A queue of files found within one GetEnumerator call.
		/// </summary>
		private sealed class CreatedFileQueue : IDisposable
		{
			readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
			readonly SemaphoreSlim _fileEnqueued = new SemaphoreSlim(0);

			/// <summary>
			/// Attempt to get a file from the queue.
			/// </summary>
			/// <param name="fileName">The name of the file, if one is immediately available.</param>
			/// <returns>True if got a file; false if not.</returns>
			public bool TryDequeue(out string fileName, CancellationToken cancellationToken)
			{
				fileName = null;
				// Avoid the OperationCanceledException if we can.
				if (cancellationToken.IsCancellationRequested)
					return false;
				try
				{
					_fileEnqueued.Wait(cancellationToken);
					return _queue.TryDequeue(out fileName);
				}
				catch (OperationCanceledException)
				{
					return false;
				}
			}

			/// <summary>
			/// Handles the Created event of the enclosing class's FileSystemWatcher.
			/// </summary>
			/// <param name="sender">This object.</param>
			/// <param name="e">Args for the new file.</param>
			public void FileCreated(object sender, FileSystemEventArgs e)
			{
				_queue.Enqueue(e.FullPath);
				_fileEnqueued.Release();
			}

			public void Dispose()
			{
				_fileEnqueued.Dispose();
			}
		}
		#endregion

		#region Constructor
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cancellationToken">This class will terminate the enumeration of
		/// files when and only when the token enters the canceled state.</param>
		/// <param name="directory">The directory to watch.</param>
		/// <param name="filePattern">A pattern to match in the file name. Example: "*.txt".
		/// Null means all files.</param>
		/// <remarks>Duplicates may be returned on the queue. See remarks for the class.</remarks>
		public CreatedFileCollection(CancellationToken cancellationToken, string directory, string filePattern=null)
		{
			Contract.Requires(directory != null);
			Contract.Requires(cancellationToken != null);

			if (!Directory.Exists(directory))
				throw new ArgumentException(String.Format("Directory '{0}' does not exist.", directory));

			_directory = directory;
			_filePattern = filePattern ?? "*";
			_cancellationToken = cancellationToken;
		}
		#endregion

		#region Methods
		/// <summary>
		/// Get an enumerator that will yield files until the CanellationToken is canceled.
		/// </summary>
		/// <returns>Fully pathed file names.</returns>
		/// <remarks>
		/// It is possible for a file name to be returned from more than once.
		/// </remarks>
		public IEnumerator<string> GetEnumerator()
		{
			if (!_cancellationToken.IsCancellationRequested)
			{
				using (var watcher = new FileSystemWatcher(_directory, _filePattern))
				{
					using (var queue = new CreatedFileQueue())
					{
						// Restrict the NotifyFilter to all that's necessary for Create events.
						// This minimizes the likelihood that FileSystemWatcher's buffer will be overwhelmed.
						watcher.NotifyFilter = NotifyFilters.FileName;

						watcher.Created += queue.FileCreated;

						watcher.EnableRaisingEvents = true;
						// Note that if a file arrives during the following loop, it will be placed on the queue
						// twice: once when the Create event is raised, and once by the loop itself.
						foreach (var file in Directory.GetFiles(_directory, _filePattern, SearchOption.TopDirectoryOnly))
						{
							queue.FileCreated(this, new FileSystemEventArgs(WatcherChangeTypes.Created, _directory, Path.GetFileName(file)));
						}

						if (!_cancellationToken.IsCancellationRequested)
						{
							string fileName;
							while (queue.TryDequeue(out fileName, _cancellationToken))
								yield return fileName;
						}
					}
				}
			}
		}

		/// <summary>
		/// Required method for IEnumerable.
		/// </summary>
		/// <returns>The generic enumerator, but as a non-generic version.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
}