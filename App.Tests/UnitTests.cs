using System.Reflection;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace App.Tests;

[TestClass]
public class MainPageSmokeTests
{
    [TestMethod]
    public void MainPage_BuildOutputAndXamlContract_ShouldContainKeyWorkbenchControls()
    {
        var appAssemblyPath = ResolveBuiltAppAssemblyPath();
        var assembly = Assembly.LoadFrom(appAssemblyPath);
        var mainPageType = assembly.GetType("App.Views.MainPage");

        Assert.IsNotNull(mainPageType, $"未找到类型 App.Views.MainPage。程序集：{appAssemblyPath}");
        Assert.IsNotNull(mainPageType!.GetConstructor(Type.EmptyTypes), "MainPage 应保留无参构造函数。");

        var xamlPath = ResolveMainPageXamlPath();
        var xaml = XDocument.Load(xamlPath);
        var nameAttributes = xaml
            .Descendants()
            .Attributes()
            .Where(attribute => attribute.Name.LocalName == "Name")
            .Select(attribute => attribute.Value)
            .ToHashSet(StringComparer.Ordinal);

        CollectionAssert.IsSubsetOf(
            new[]
            {
                "ImageModeButton",
                "UiModeButton",
                "DeviceList",
                "Canvas",
                "CaptureButton"
            },
            nameAttributes.ToList(),
            $"MainPage.xaml 缺少关键控件。文件：{xamlPath}");
    }

    private static string ResolveBuiltAppAssemblyPath()
    {
        var solutionRoot = GetSolutionRoot();
        var appBinDirectory = Path.Combine(solutionRoot, "App", "bin");

        Assert.IsTrue(Directory.Exists(appBinDirectory), $"未找到 App 输出目录：{appBinDirectory}");

        var candidate = Directory
            .EnumerateFiles(appBinDirectory, "autojs6-dev-tools.dll", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(GetAssemblyPathPriority)
            .ThenByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        Assert.IsFalse(string.IsNullOrWhiteSpace(candidate), $"未找到已构建的 App 程序集，请先构建解决方案。目录：{appBinDirectory}");

        return candidate!;
    }

    private static string ResolveMainPageXamlPath()
    {
        var solutionRoot = GetSolutionRoot();
        var xamlPath = Path.Combine(solutionRoot, "App", "Views", "MainPage.xaml");

        Assert.IsTrue(File.Exists(xamlPath), $"未找到 MainPage.xaml：{xamlPath}");

        return xamlPath;
    }

    private static string GetSolutionRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }

    private static int GetAssemblyPathPriority(string path)
    {
        if (path.Contains($"{Path.DirectorySeparatorChar}x64{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (path.Contains($"{Path.DirectorySeparatorChar}ARM64{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 2;
    }
}
