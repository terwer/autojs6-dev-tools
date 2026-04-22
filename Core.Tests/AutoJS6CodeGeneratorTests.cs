using Core.Models;
using Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Core.Tests;

[TestClass]
public class AutoJS6CodeGeneratorTests
{
    [TestMethod]
    public void GenerateImageModeCode_ShouldUseVarRegionAndTemplateRecycle()
    {
        var generator = new AutoJS6CodeGenerator();
        var options = new AutoJS6CodeOptions
        {
            Mode = CodeGenerationMode.Image,
            VariablePrefix = "target",
            TemplatePath = "./assets/login.png",
            Threshold = 0.85,
            Region = new CropRegion
            {
                X = 100,
                Y = 200,
                Width = 300,
                Height = 400
            },
            GenerateRetryLogic = false,
            GenerateImageRecycle = true
        };

        var code = generator.GenerateImageModeCode(options);

        StringAssert.Contains(code, "var targetTemplate = images.read(\"./assets/login.png\");");
        StringAssert.Contains(code, "var result = images.findImage(screen, targetTemplate, {");
        StringAssert.Contains(code, "region: [100, 200, 300, 400]");
        StringAssert.Contains(code, "targetTemplate.recycle();");
        Assert.IsFalse(code.Contains("const ", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("let ", StringComparison.Ordinal));
    }

    [TestMethod]
    public void GenerateWidgetModeCode_ShouldKeepIdTextDescFallbackOrder()
    {
        var generator = new AutoJS6CodeGenerator();
        var widget = new WidgetNode
        {
            ClassName = "android.widget.Button",
            ResourceId = "demo:id/start",
            Text = "开始",
            ContentDesc = "开始按钮",
            Clickable = true,
            Bounds = "[300,400][520,500]",
            BoundsRect = (300, 400, 220, 100),
            Package = "demo",
            Depth = 1
        };

        var options = new AutoJS6CodeOptions
        {
            Mode = CodeGenerationMode.Widget,
            VariablePrefix = "targetWidget",
            Widget = widget,
            GenerateRetryLogic = false
        };

        var code = generator.GenerateWidgetModeCode(options);

        var idIndex = code.IndexOf("id(\"demo:id/start\")", StringComparison.Ordinal);
        var textIndex = code.IndexOf("text(\"开始\")", StringComparison.Ordinal);
        var descIndex = code.IndexOf("desc(\"开始按钮\")", StringComparison.Ordinal);

        Assert.IsTrue(idIndex >= 0, "应优先生成 id 选择器。");
        Assert.IsTrue(textIndex > idIndex, "text 应作为 id 失败后的降级选择器。");
        Assert.IsTrue(descIndex > textIndex, "desc 应作为 text 失败后的降级选择器。");

        StringAssert.Contains(code, "if (!targetWidget) {");
        StringAssert.Contains(code, "boundsInside(300, 400, 520, 500).findOne()");
    }
}
