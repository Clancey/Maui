using System;
using System.Collections.Generic;
using System.Text;
using Android.Animation;
using Android.Content;
using Android.OS;
using Microsoft.Maui.Animations;

namespace Microsoft.Maui
{
	public class AndroidTicker : Ticker
	{
		ValueAnimator _val;
		bool _systemEnabled;
		private IMauiContext? _context;

		public AndroidTicker()
		{
			_val = new ValueAnimator();
			_val.SetIntValues(0, 100); // avoid crash
			_val.RepeatCount = ValueAnimator.Infinite;
			_val.Update += (s, e) => Fire?.Invoke();
		}

		internal void CheckPowerSaveModeStatus()
		{
			// Android disables animations when it's in power save mode
			// So we need to keep track of whether we're in that mode and handle animations accordingly
			// We can't just check ValueAnimator.AreAnimationsEnabled() because there's no event for that, and it's
			// only supported on API >= 26

			//if (!Forms.IsLollipopOrNewer)
			//{
			//    _systemEnabled = true;
			//    return;
			//}

			var powerManager = MauiContext.Context!.GetSystemService(Context.PowerService) as PowerManager;

			var powerSaveOn = powerManager?.IsPowerSaveMode ?? false;

			// If power saver is active, then animations will not run
			_systemEnabled = !powerSaveOn;

		}

		public IMauiContext MauiContext { 
			get => _context ?? throw new ArgumentNullException("Maui Context is not set");
			set
			{
				_context = value;
				CheckPowerSaveModeStatus();
			}
		}

		public override bool IsRunning => _val.IsStarted;
		public override bool SystemEnabled { get => _systemEnabled; }
		public override void Start() => _val.Start();
		public override void Stop()
		{

			// ThreadHelper.RunOnMainThread(() => _val?.Cancel());
		}
	}
}