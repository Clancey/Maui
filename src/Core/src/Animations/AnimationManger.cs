using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Maui.Animations
{
	public class AnimationManger
	{
		public double SpeedModifier { get; set; } = 1;

		public AnimationManger(ITicker ticker)
		{
			var isRunning = Ticker?.IsRunning ?? false;
			Ticker = ticker;
			ticker.Fire = OnFire;
			if (isRunning)
				ticker.Start();
			lastUpdate = GetCurrentTick();
		}
		long lastUpdate;
	

		ITicker Ticker;

		static List<Animation> Animations = new List<Animation>();
		public void Add(Animation animation)
		{
			//If animations are disabled, don't do anything
			if (!Ticker.SystemEnabled)
			{
				return;
			}
			if (!Animations.Contains(animation))
				Animations.Add(animation);
			if (!Ticker.IsRunning)
				Start();
		}

		public void Remove(Animation animation)
		{
			Animations.TryRemove(animation);
			if (!Animations.Any())
				End();
		}

		void Start()
		{
			lastUpdate = GetCurrentTick();
			Ticker.Start();
		}

		long GetCurrentTick() => (Environment.TickCount & int.MaxValue);

		void End() => Ticker?.Stop();
		void OnFire()
		{
			var now = GetCurrentTick();
			var seconds = TimeSpan.FromMilliseconds((now - lastUpdate)).TotalSeconds;
			lastUpdate = now;
			var animations = Animations.ToList();
			void animationTick(Animation animation)
			{
				if (animation.HasFinished)
				{
					Animations.TryRemove(animation);
					animation.RemoveFromParent();
					return;
				}

				animation.Tick(seconds * SpeedModifier);
				if (animation.HasFinished)
				{
					Animations.TryRemove(animation);
					animation.RemoveFromParent();
				}
			}
			animations.ForEach(animationTick);

			if (!Animations.Any())
				End();
		}

	}
}
