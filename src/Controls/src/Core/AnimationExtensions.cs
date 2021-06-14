using System;
using System.Collections.Generic;
using Microsoft.Maui.Animations;
using Microsoft.Maui.Controls.Internals;

namespace Microsoft.Maui.Controls
{
	public static class AnimationExtensions
	{
		static readonly Dictionary<AnimatableKey, Animation> s_animations;
		static readonly Dictionary<AnimatableKey, int> s_kinetics;

		static AnimationExtensions()
		{
			s_animations = new Dictionary<AnimatableKey, Animation>();
			s_kinetics = new Dictionary<AnimatableKey, int>();
		}

		public static void Commit(this Animation animation, IFrameworkElement owner, string name, uint rate = 16, uint length = 250, Easing easing = null, Action<double, bool> finished = null, Func<bool> repeat = null)
		{
			//TODO: Get Animation

		}
		public static void Commit(this Animation animation, IAnimatable owner, AnimationManger manager, string name, uint rate = 16, uint length = 250, Easing easing = null, Action<double, bool> finished = null, Func<bool> repeat = null)
		{
			animation.Name = name;
			animation.Easing = easing ?? Easing.Default;
			animation.Duration = 1000d / length;
			animation.Finished = ()=> finished?.Invoke(animation.Progress, animation.HasFinished);
			DoAction(owner, () => manager.Add(animation));			
		}

		public static bool AbortAnimation(this IAnimatable self, string handle)
		{
			var key = new AnimatableKey(self, handle);

			if (!s_animations.ContainsKey(key) && !s_kinetics.ContainsKey(key))
			{
				return false;
			}

			Action abort = () =>
			{
				AbortAnimation(key);
				AbortKinetic(key);
			};

			DoAction(self, abort);

			return true;
		}
		
		public static void Animate(this IAnimatable self, string name, Animation animation, AnimationManger manager, uint length = 250, Easing easing = null)
		{
			animation.Easing = easing ?? Easing.Default;
			animation.Name = name;
			animation.Duration = 1000d / length;
			DoAction(self, ()=> manager.Add(animation));
		}
		public static void Animate(this IFrameworkElement frameworkElement, Animation animation)
		{
			//TODO: Get Animation Manager from framework element;
			
		}

		public static void AnimateKinetic(this IAnimatable self, string name, Func<double, double, bool> callback, double velocity, double drag, Action finished = null)
		{
			Action animate = () => AnimateKineticInternal(self, name, callback, velocity, drag, finished);
			DoAction(self, animate);
		}

		public static bool AnimationIsRunning(this IAnimatable self, string handle)
		{
			var key = new AnimatableKey(self, handle);
			return s_animations.ContainsKey(key);
		}

		public static Func<double, double> Interpolate(double start, double end = 1.0f, double reverseVal = 0.0f, bool reverse = false)
		{
			double target = reverse ? reverseVal : end;
			return x => start + (target - start) * x;
		}

		public static IDisposable Batch(this IAnimatable self) => new BatchObject(self);

		static void AbortAnimation(AnimatableKey key)
		{
			// If multiple animations on the same view with the same name (IOW, the same AnimatableKey) are invoked
			// asynchronously (e.g., from the `[Animate]To` methods in `ViewExtensions`), it's possible to get into 
			// a situation where after invoking the `Finished` handler below `s_animations` will have a new `Info`
			// object in it with the same AnimatableKey. We need to continue cancelling animations until that is no
			// longer the case; thus, the `while` loop.

			// If we don't cancel all of the animations popping in with this key, `AnimateInternal` will overwrite one
			// of them with the new `Info` object, and the overwritten animation will never complete; any `await` for
			// it will never return.

			while (s_animations.ContainsKey(key))
			{
				
				var animation = s_animations[key];
				s_animations.Remove(key);
				animation.Pause();
				animation.RemoveFromParent();
				animation.Finished?.Invoke();
			}
		}

		static void AbortKinetic(AnimatableKey key)
		{
			if (!s_kinetics.ContainsKey(key))
			{
				return;
			}

			
			Ticker.Default.Remove(s_kinetics[key]);
			s_kinetics.Remove(key);
		}

		static void AnimateInternal(IAnimatable self, Animation animation, AnimationManger manager)
		{
			animation.Finished += () =>
			{
			};
		}
		static void AnimateInternal1<T>(IAnimatable self, string name, Func<double, T> transform, Action<T> callback,
			uint rate, uint length, Easing easing, Action<T, bool> finished, Func<bool> repeat)
		{
			var key = new AnimatableKey(self, name);

			AbortAnimation(key);

			Action<double> step = f => callback(transform(f));
			Action<double, bool> final = null;
			if (finished != null)
				final = (f, b) => finished(transform(f), b);

			var info = new Info { Rate = rate, Length = length, Easing = easing ?? Easing.Linear };

			var tweener = new Tweener(info.Length, info.Rate);
			tweener.Handle = key;
			tweener.ValueUpdated += HandleTweenerUpdated;
			tweener.Finished += HandleTweenerFinished;

			info.Tweener = tweener;
			info.Callback = step;
			info.Finished = final;
			info.Repeat = repeat;
			info.Owner = new WeakReference<IAnimatable>(self);

			s_animations[key] = info;

			info.Callback(0.0f);
			tweener.Start();
		}

		static void AnimateKineticInternal(IAnimatable self, string name, Func<double, double, bool> callback, double velocity, double drag, Action finished = null)
		{
			var key = new AnimatableKey(self, name);

			AbortKinetic(key);

			double sign = velocity / Math.Abs(velocity);
			velocity = Math.Abs(velocity);

			int tick = Ticker.Default.Insert(step =>
			{
				long ms = step;

				velocity -= drag * ms;
				velocity = Math.Max(0, velocity);

				var result = false;
				if (velocity > 0)
				{
					result = callback(sign * velocity * ms, velocity);
				}

				if (!result)
				{
					finished?.Invoke();
					s_kinetics.Remove(key);
				}
				return result;
			});

			s_kinetics[key] = tick;
		}

		static void HandleAnimationFinished(Animation animation)
		{
			s_animations.TryAdd
			if (tweener != null && s_animations.TryGetValue(tweener.Handle, out info))
			{
				IAnimatable owner;
				if (info.Owner.TryGetTarget(out owner))
					owner.BatchBegin();
				info.Callback(tweener.Value);

				var repeat = false;

				// If the Ticker has been disabled (e.g., by power save mode), then don't repeat the animation
				var animationsEnabled = Ticker.Default.SystemEnabled;

				if (info.Repeat != null && animationsEnabled)
					repeat = info.Repeat();

				if (!repeat)
				{
					s_animations.Remove(tweener.Handle);
					tweener.ValueUpdated -= HandleTweenerUpdated;
					tweener.Finished -= HandleTweenerFinished;
				}

				info.Finished?.Invoke(tweener.Value, !animationsEnabled);

				if (info.Owner.TryGetTarget(out owner))
					owner.BatchCommit();

				if (repeat)
				{
					tweener.Start();
				}
			}
		}

		static void HandleTweenerUpdated(object o, EventArgs args)
		{
			var tweener = o as Tweener;
			Info info;
			IAnimatable owner;

			if (tweener != null && s_animations.TryGetValue(tweener.Handle, out info) && info.Owner.TryGetTarget(out owner))
			{
				owner.BatchBegin();
				info.Callback(info.Easing.Ease(tweener.Value));
				owner.BatchCommit();
			}
		}

		static void DoAction(IAnimatable self, Action action)
		{
			if (self is BindableObject element)
			{
				if (element.Dispatcher.IsInvokeRequired)
				{
					element.Dispatcher.BeginInvokeOnMainThread(action);
				}
				else
				{
					action();
				}

				return;
			}

			if (Device.IsInvokeRequired)
			{
				Device.BeginInvokeOnMainThread(action);
			}
			else
			{
				action();
			}
		}

		
		sealed class BatchObject : IDisposable
		{
			IAnimatable _animatable;

			public BatchObject(IAnimatable animatable)
			{
				_animatable = animatable;
				_animatable?.BatchBegin();
			}

			public void Dispose()
			{
				_animatable?.BatchCommit();
				_animatable = null;
			}
		}
	}
}