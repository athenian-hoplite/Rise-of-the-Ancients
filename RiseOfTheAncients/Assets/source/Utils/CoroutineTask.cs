﻿/// TaskManager.cs
/// Copyright (c) 2011, Ken Rockot  <k-e-n-@-REMOVE-CAPS-AND-HYPHENS-oz.gs>.  All rights reserved.
/// Everyone is granted non-exclusive license to do anything at all with this code.
///
/// This is a new coroutine interface for Unity.
///
/// The motivation for this is twofold:
///
/// 1. The existing coroutine API provides no means of stopping specific
///    coroutines; StopCoroutine only takes a string argument, and it stops
///    all coroutines started with that same string; there is no way to stop
///    coroutines which were started directly from an enumerator.  This is
///    not robust enough and is also probably pretty inefficient.
///
/// 2. StartCoroutine and friends are MonoBehaviour methods.  This means
///    that in order to start a coroutine, a user typically must have some
///    component reference handy.  There are legitimate cases where such a
///    constraint is inconvenient.  This implementation hides that
///    constraint from the user.
///
/// Example usage:
///
/// ----------------------------------------------------------------------------
/// IEnumerator MyAwesomeTask()
/// {
///     while(true) {
///         Debug.Log("Logcat iz in ur consolez, spammin u wif messagez.");
///         yield return null;
////    }
/// }
///
/// IEnumerator TaskKiller(float delay, Task t)
/// {
///     yield return new WaitForSeconds(delay);
///     t.Stop();
/// }
///
/// void SomeCodeThatCouldBeAnywhereInTheUniverse()
/// {
///     Task spam = new Task(MyAwesomeTask());
///     new Task(TaskKiller(5, spam));
/// }
/// ----------------------------------------------------------------------------
///
/// When SomeCodeThatCouldBeAnywhereInTheUniverse is called, the debug console
/// will be spammed with annoying messages for 5 seconds.
///
/// Simple, really.  There is no need to initialize or even refer to TaskManager.
/// When the first Task is created in an application, a "TaskManager" GameObject
/// will automatically be added to the scene root with the TaskManager component
/// attached.  This component will be responsible for dispatching all coroutines
/// behind the scenes.
///
/// Task also provides an event that is triggered when the coroutine exits.

using UnityEngine;
using System.Collections;

/// <summary>
/// I renamed Task to CoroutineTask and TaskManager to CoroutineTaskManager to avoid confusion with System.Threading.Tasks.
/// Added and refactored a few comments and added a namespace. These were my only changes to this class which was written by Ken Rockot.
/// 
/// Renato Pereira
/// </summary>
namespace ROTA.Utils
{	
 
/// <summary>
/// A CoroutineTask object represents a coroutine.  Coroutines can be started, paused, and stopped.
/// It is an error to attempt to start a coroutine that has been stopped or which has
/// naturally terminated. 
/// </summary>
public class CoroutineTask
{
	
	/// <summary>
	/// Returns true if and only if the coroutine is running.  Paused coroutines
	/// are considered to be running. 
	/// </summary>
	public bool Running {
		get {
			return task.Running;
		}
	}
	
	/// <summary>
	/// Returns true if and only if the coroutine is currently paused. 
	/// </summary>
	public bool Paused {
		get {
			return task.Paused;
		}
	}
	
	/// <summary>
	/// Delegate for termination subscribers.  manual is true if and only if
	/// the coroutine was stopped with an explicit call to Stop(). 
	/// </summary>
	public delegate void FinishedHandler(bool manual);
	
	/// <summary>
	/// Termination event.  Triggered when the coroutine completes execution. 
	/// </summary>
	public event FinishedHandler Finished;
	
	/// <summary>
	/// Creates a new CoroutineTask object for the given coroutine.
	///
	/// If autoStart is true (default) the coroutine is automatically started
	/// upon construction. 
	/// </summary>
	public CoroutineTask(IEnumerator c, bool autoStart = true)
	{
		task = CoroutineTaskManager.CreateTask(c);
		task.Finished += TaskFinished;
		if(autoStart)
			Start();
	}
	
	/// <summary>
	/// Begins execution of the coroutine. 
	/// </summary>
	public void Start()
	{
		task.Start();
	}

	/// <summary>
	/// Discontinues execution of the coroutine at its next yield. 
	/// </summary>
	public void Stop()
	{
		task.Stop();
	}
	
	/// <summary>
	/// Pause coroutine execution.
	/// </summary>
	public void Pause()
	{
		task.Pause();
	}
	
	/// <summary>
	/// Resume coroutine execution.
	/// </summary>
	public void Unpause()
	{
		task.Unpause();
	}
	
	void TaskFinished(bool manual)
	{
		FinishedHandler handler = Finished;
		if(handler != null)
			handler(manual);
	}
	
	CoroutineTaskManager.CoroutineTaskState task;
}

class CoroutineTaskManager : MonoBehaviour
{
	public class CoroutineTaskState
	{
		public bool Running {
			get {
				return running;
			}
		}

		public bool Paused  {
			get {
				return paused;
			}
		}

		public delegate void FinishedHandler(bool manual);
		public event FinishedHandler Finished;

		IEnumerator coroutine;
		bool running;
		bool paused;
		bool stopped;
		
		public CoroutineTaskState(IEnumerator c)
		{
			coroutine = c;
		}
		
		public void Pause()
		{
			paused = true;
		}
		
		public void Unpause()
		{
			paused = false;
		}
		
		public void Start()
		{
			running = true;
			singleton.StartCoroutine(CallWrapper());
		}
		
		public void Stop()
		{
			stopped = true;
			running = false;
		}
		
		IEnumerator CallWrapper()
		{
			yield return null;
			IEnumerator e = coroutine;
			while(running) {
				if(paused)
					yield return null;
				else {
					if(e != null && e.MoveNext()) {
						yield return e.Current;
					}
					else {
						running = false;
					}
				}
			}
			
			FinishedHandler handler = Finished;
			if(handler != null)
				handler(stopped);
		}
	}

	static CoroutineTaskManager singleton;

	public static CoroutineTaskState CreateTask(IEnumerator coroutine)
	{
		if(singleton == null) {
			GameObject go = new GameObject("CoroutineTaskManager");
			singleton = go.AddComponent<CoroutineTaskManager>();
		}
		return new CoroutineTaskState(coroutine);
	}
}

}