using System.Collections.Generic;
using Microsoft.Maui;
using Microsoft.Maui.Layouts;
using NSubstitute;
using Xunit;
using Microsoft.Maui.Primitives;

namespace Microsoft.Maui.UnitTests.Layouts
{
	[Category(TestCategory.Core, TestCategory.Layout)]
	public class HorizontalStackLayoutManagerTests : StackLayoutManagerTests
	{
		[Theory]
		[InlineData(0, 100, 0, 0)]
		[InlineData(1, 100, 0, 100)]
		[InlineData(1, 100, 13, 100)]
		[InlineData(2, 100, 13, 213)]
		[InlineData(3, 100, 13, 326)]
		[InlineData(3, 100, -13, 274)]
		public void SpacingMeasurement(int viewCount, double viewWidth, int spacing, double expectedWidth)
		{
			var stack = BuildStack(viewCount, viewWidth, 100);
			stack.Spacing.Returns(spacing);

			var manager = new HorizontalStackLayoutManager(stack);
			var measuredSize = manager.Measure(double.PositiveInfinity, 100);

			Assert.Equal(expectedWidth, measuredSize.Width);
		}

		[Theory("Spacing should not affect arrangement with only one item")]
		[InlineData(0), InlineData(26), InlineData(-54)]
		public void SpacingArrangementOneItem(int spacing)
		{
			var stack = BuildStack(1, 100, 100);
			stack.Spacing.Returns(spacing);

			var manager = new HorizontalStackLayoutManager(stack);

			var measuredSize = manager.Measure(double.PositiveInfinity, 100);
			manager.Arrange(new Rectangle(Point.Zero, measuredSize));

			var expectedRectangle = new Rectangle(0, 0, 100, 100);
			stack.Children[0].Received().Arrange(Arg.Is(expectedRectangle));
		}

		[Theory("Spacing should affect arrangement with more than one item")]
		[InlineData(26), InlineData(-54)]
		public void SpacingArrangementTwoItems(int spacing)
		{
			var stack = BuildStack(2, 100, 100);
			stack.Spacing.Returns(spacing);

			var manager = new HorizontalStackLayoutManager(stack);

			var measuredSize = manager.Measure(double.PositiveInfinity, 100);
			manager.Arrange(new Rectangle(Point.Zero, measuredSize));

			var expectedRectangle0 = new Rectangle(0, 0, 100, 100);
			stack.Children[0].Received().Arrange(Arg.Is(expectedRectangle0));

			var expectedRectangle1 = new Rectangle(100 + spacing, 0, 100, 100);
			stack.Children[1].Received().Arrange(Arg.Is(expectedRectangle1));
		}

		[Theory]
		[InlineData(150, 100, 100)]
		[InlineData(150, 200, 200)]
		[InlineData(1250, -1, 1250)]
		public void StackAppliesWidth(double viewWidth, double stackWidth, double expectedWidth)
		{
			var stack = CreateTestLayout();

			var view = CreateTestView(new Size(viewWidth, 100));

			var children = new List<IView>() { view }.AsReadOnly();

			stack.Children.Returns(children);
			stack.Width.Returns(stackWidth);

			var manager = new HorizontalStackLayoutManager(stack);
			var measurement = manager.Measure(double.PositiveInfinity, 100);
			Assert.Equal(expectedWidth, measurement.Width);
		}
	}
}
