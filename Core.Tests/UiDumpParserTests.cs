using Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Core.Tests;

[TestClass]
public class UiDumpParserTests
{
    [TestMethod]
    public async Task ParseAsync_AndFilterNodes_ShouldSkipRedundantLayoutContainers()
    {
        const string xml = """
            <hierarchy rotation="0">
              <node class="android.widget.FrameLayout" resource-id="" text="" content-desc="" clickable="false" bounds="[0,0][1280,720]" package="demo">
                <node class="android.widget.LinearLayout" resource-id="" text="" content-desc="" clickable="false" bounds="[0,0][1280,720]" package="demo">
                  <node class="android.widget.TextView" resource-id="demo:id/title" text="开始战斗" content-desc="" clickable="false" bounds="[40,50][220,110]" package="demo" />
                  <node class="android.widget.Button" resource-id="demo:id/start" text="开始" content-desc="开始按钮" clickable="true" bounds="[300,400][520,500]" package="demo" />
                </node>
              </node>
            </hierarchy>
            """;

        var parser = new UiDumpParser();

        var root = await parser.ParseAsync(xml);

        Assert.IsNotNull(root);
        Assert.AreEqual("android.widget.FrameLayout", root.ClassName);
        Assert.AreEqual((0, 0, 1280, 720), root.BoundsRect);

        var filteredNodes = parser.FilterNodes(root);

        Assert.AreEqual(2, filteredNodes.Count);
        Assert.AreEqual("demo:id/title", filteredNodes[0].ResourceId);
        Assert.AreEqual("demo:id/start", filteredNodes[1].ResourceId);
    }

    [TestMethod]
    public async Task FindNodeByCoordinate_ShouldReturnDeepestMatchingNode()
    {
        const string xml = """
            <hierarchy rotation="0">
              <node class="android.widget.FrameLayout" resource-id="" text="" content-desc="" clickable="false" bounds="[0,0][1280,720]" package="demo">
                <node class="android.widget.LinearLayout" resource-id="" text="" content-desc="" clickable="false" bounds="[0,0][1280,720]" package="demo">
                  <node class="android.widget.ImageView" resource-id="demo:id/banner" text="" content-desc="横幅" clickable="false" bounds="[20,20][400,200]" package="demo" />
                  <node class="android.widget.Button" resource-id="demo:id/confirm" text="确认" content-desc="确认按钮" clickable="true" bounds="[500,300][760,420]" package="demo" />
                </node>
              </node>
            </hierarchy>
            """;

        var parser = new UiDumpParser();
        var root = await parser.ParseAsync(xml);

        Assert.IsNotNull(root);

        var node = parser.FindNodeByCoordinate(root, 550, 350);

        Assert.IsNotNull(node);
        Assert.AreEqual("demo:id/confirm", node.ResourceId);
        Assert.AreEqual((500, 300, 260, 120), node.BoundsRect);
    }

    [TestMethod]
    public async Task ParseAsync_InvalidXml_ShouldReturnNull()
    {
        var parser = new UiDumpParser();

        var root = await parser.ParseAsync("<hierarchy><node>");

        Assert.IsNull(root);
    }
}
