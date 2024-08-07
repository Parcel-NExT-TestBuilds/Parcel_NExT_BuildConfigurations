namespace Zora.Infrastructure.Package
{
    /// <summary>
    /// Helper class for scaffolding new .Net packages
    /// </summary>
    public static class PackageTemplateGenerator
    {
        public static string GenerateTemplate(string packageName, string folderPath)
        {
            packageName = packageName.Replace(" ", string.Empty);
            string projectFolder = Path.Combine(folderPath, packageName);

            // Generate something that is similar to `dotnet new console` without relying on dotnet sdk, aka. just use plain text
            Directory.CreateDirectory(projectFolder);
            File.WriteAllText(Path.Combine(projectFolder, $"{packageName}.csproj"), $$"""
                <Project Sdk="Microsoft.NET.Sdk">

                  <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    <RootNamespace>{{packageName}}</RootNamespace>
                    <Version>1.0.0</Version>
                    <SatelliteResourceLanguages>en-US</SatelliteResourceLanguages>
                    <GenerateDocumentationFile>true</GenerateDocumentationFile>
                  </PropertyGroup>

                </Project>
                """);
            File.WriteAllText(Path.Combine(projectFolder, $"Program.cs"), $$"""
                namespace {{packageName}}
                {
                    public static class MyClass
                    {
                        public static string HelloWorld() 
                            => "Hello World!";
                    }
                }
                """);

            return projectFolder;
        }
    }
}
