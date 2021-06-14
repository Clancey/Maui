using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.Maui.Animations
{
	public class Animation : IDisposable, IEnumerable
	{
		//TODO: Add a way to cancel animations
		//TODO: Add a way to find an animation by ID
		//TODO: Allow us to prevent an animation from auto disposing

		public Animation()
		{

		}
		public Animation(Action<double> callback, double start = 0.0f, double end = 1.0f, Easing? easing = null, Action? finished = null)
		{
			childrenAnimations = new List<Animation>();
			Finished = finished;
			Easing = easing ?? Easing.Default;
			_step = callback;
			StartValue = start;
			EndValue = end;

		}
		public Animation(List<Animation> animations)
		{
			childrenAnimations = animations;
		}
		internal WeakReference<IAnimator>? Parent { get; set; }

		public Action? Finished { get; set; }
		Action<double>? _step;
		public Action? ValueChanged { get; set; }
		public string? Name { get; set; }
		bool paused;
		public bool IsPaused => paused;
		List<Animation> childrenAnimations = new List<Animation>();
		public double StartDelay { get; set; }
		public double Duration { get; set; }
		public double CurrentTime { get; protected set; }
		public double Progress { get; protected set; }
		public Easing Easing { get; set; } = Easing.Default;
		public bool HasFinished { get; protected set; }
		public bool Repeats { get; set; }
		double skippedSeconds;
		int usingResource = 0;
		public object? StartValue { get; set; }
		public object? EndValue { get; set; }
		public object? CurrentValue { get; protected set; }
		Lerp? _lerp;
		public Lerp? Lerp
		{
			get
			{
				if (_lerp != null)
					return _lerp;

				//TODO: later we should find the first matching types of the subclasses
				var type = StartValue?.GetType() ?? EndValue?.GetType();
				if (type == null)
					return null;
				return _lerp = Lerp.GetLerp(type);
			}
			set => _lerp = value;
		}


		public IEnumerator GetEnumerator() => childrenAnimations.GetEnumerator();

		public void Add(double beginAt, double finishAt, Animation animation)
		{
			if (beginAt < 0 || beginAt > 1)
				throw new ArgumentOutOfRangeException("beginAt");

			if (finishAt < 0 || finishAt > 1)
				throw new ArgumentOutOfRangeException("finishAt");

			if (finishAt <= beginAt)
				throw new ArgumentException("finishAt must be greater than beginAt");

			animation.StartDelay = beginAt;
			animation.Duration = finishAt - beginAt;
			childrenAnimations.Add(animation);
		}

		public void Tick(double secondsSinceLastUpdate)
		{
			if (IsPaused)
				return;

			if (0 == Interlocked.Exchange(ref usingResource, 1))
			{
				try
				{
					OnTick(skippedSeconds + secondsSinceLastUpdate);
					skippedSeconds = 0;
				}
				finally
				{
					//Release the lock
					Interlocked.Exchange(ref usingResource, 0);
				}
			}
			//animation is lagging behind!
			else
			{
				skippedSeconds += secondsSinceLastUpdate;
			}
		}
		AnimationManger? animationManger;

		protected virtual void OnTick(double secondsSinceLastUpdate)
		{
			if (HasFinished)
				return;

			CurrentTime += secondsSinceLastUpdate;
			_step?.Invoke(CurrentTime);
			if (childrenAnimations.Any())
			{
				var hasFinished = true;
				foreach (var animation in childrenAnimations)
				{

					animation.OnTick(secondsSinceLastUpdate);
					if (!animation.HasFinished)
						hasFinished = false;

				}
				HasFinished = hasFinished;


			}
			else
			{

				var start = CurrentTime - StartDelay;
				if (CurrentTime < StartDelay)
					return;
				var percent = Math.Min(start / Duration, 1);
				Progress = percent;
				Update(percent);
			}
			if (HasFinished)
			{
				Finished?.Invoke();
				if(Repeats)
					Reset();
			}
		}

		public virtual void Update(double percent)
		{
			try
			{
				var progress = Easing.Ease(percent);

				if (Lerp != null! && StartValue != null && EndValue != null)
					CurrentValue = Lerp.Calculate?.Invoke(StartValue, EndValue, progress);
				_step?.Invoke(progress);
				ValueChanged?.Invoke();
				HasFinished = percent == 1;
			}
			catch (Exception ex)
			{
				//TODO log exception
				Console.WriteLine(ex);
				HasFinished = true;
			}
		}
		public void Commit(AnimationManger animationManger)
		{
			this.animationManger = animationManger;
			animationManger.Add(this);
		}

		public Animation CreateAutoReversing()
		{
			var reveresedChildren = childrenAnimations.ToList();
			reveresedChildren.Reverse();
			var reveresed = CreateReverse();
			var parentAnimation = new Animation
			{
				Duration = reveresed.StartDelay + reveresed.Duration,
				Repeats = Repeats,
				childrenAnimations =
				{
					this,
					reveresed,
				}
			};
			Repeats = false;
			return parentAnimation;
		}


		public void Reset()
		{
			CurrentTime = 0;
			HasFinished = false;
			foreach (var x in childrenAnimations)
				x.Reset();
		}

		public void Pause()
		{
			paused = true;
			animationManger?.Remove(this);
		}
		public void Resume()
		{
			paused = false;
			animationManger?.Add(this);
		}
		public void RemoveFromParent()
		{
			IAnimator? view = null;
			if (this.Parent?.TryGetTarget(out view) ?? false)
				view?.RemoveAnimation(this);
		}


		protected virtual Animation CreateReverse()
		{
			var reveresedChildren = childrenAnimations.ToList();
			reveresedChildren.Reverse();
			return new Animation
			{
				Easing = Easing,
				Duration = Duration,
				StartDelay = StartDelay + Duration,
				childrenAnimations = reveresedChildren,
			};
		}

		public Animation Insert(double beginAt, double finishAt, Animation animation)
		{
			Add(beginAt, finishAt, animation);
			return this;
		}

		public Animation WithConcurrent(Animation animation, double beginAt = 0.0f, double finishAt = 1.0f)
		{
			animation.StartDelay = beginAt;
			animation.Duration = finishAt - beginAt;
			childrenAnimations.Add(animation);
			return this;
		}

		public Animation WithConcurrent(Action<double> callback, double start = 0.0f, double end = 1.0f, Easing? easing = null, double beginAt = 0.0f, double finishAt = 1.0f)
		{
			var child = new Animation(callback, start, end, easing);
			child.StartDelay = beginAt;
			child.Duration = finishAt - beginAt;
			childrenAnimations.Add(child);
			return this;
		}

		#region IDisposable Support
		public bool IsDisposed => disposedValue;


		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					foreach (var child in childrenAnimations)
						child.Dispose();
					childrenAnimations.Clear();
				}
				disposedValue = true;
				animationManger?.Remove(this);
				Finished = null;
				_step = null;
			}
		}


		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}

		#endregion
	}
}