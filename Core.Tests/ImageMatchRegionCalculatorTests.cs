using Core.Helpers;
using Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Core.Tests;

[TestClass]
public class ImageMatchRegionCalculatorTests
{
    [TestMethod]
    public void Create_LandscapeReference_ShouldClampAndGenerateRegionRef()
    {
        var referenceBounds = new CropRegion
        {
            X = 1200,
            Y = 680,
            Width = 200,
            Height = 80,
            OriginalWidth = 1280,
            OriginalHeight = 720
        };

        var context = ImageMatchRegionCalculator.Create(referenceBounds, padding: 30);

        Assert.AreEqual("landscape", context.Orientation);
        Assert.AreEqual(1200, context.ReferenceBounds.X);
        Assert.AreEqual(680, context.ReferenceBounds.Y);
        Assert.AreEqual(200, context.ReferenceBounds.Width);
        Assert.AreEqual(80, context.ReferenceBounds.Height);
        Assert.AreEqual(1170, context.SearchRegion.X);
        Assert.AreEqual(650, context.SearchRegion.Y);
        Assert.AreEqual(110, context.SearchRegion.Width);
        Assert.AreEqual(70, context.SearchRegion.Height);
        CollectionAssert.AreEqual(new[] { 1170, 650, 110, 70 }, context.RegionRef);
    }

    [TestMethod]
    public void Create_PortraitReference_ShouldScaleTo720x1280()
    {
        var referenceBounds = new CropRegion
        {
            X = 100,
            Y = 200,
            Width = 120,
            Height = 240,
            OriginalWidth = 1080,
            OriginalHeight = 1920
        };

        var context = ImageMatchRegionCalculator.Create(referenceBounds, padding: 20);

        Assert.AreEqual("portrait", context.Orientation);
        Assert.AreEqual(80, context.SearchRegion.X);
        Assert.AreEqual(180, context.SearchRegion.Y);
        Assert.AreEqual(160, context.SearchRegion.Width);
        Assert.AreEqual(280, context.SearchRegion.Height);
        CollectionAssert.AreEqual(new[] { 53, 120, 107, 187 }, context.RegionRef);
    }
}
