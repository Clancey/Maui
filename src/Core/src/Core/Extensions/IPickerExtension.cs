﻿using System;
using System.Linq;
namespace Microsoft.Maui
{
	public static class IPickerExtension
	{
		public static string[] GetItemsAsArray(this IPicker picker) => Enumerable.Range(0, picker.Count()).Select(i => picker.DisplayFor(i)).ToArray();
	}
}
