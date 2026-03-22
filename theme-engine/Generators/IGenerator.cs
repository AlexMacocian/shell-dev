namespace ThemeEngine.Generators;

public interface IGenerator
{
    string Name { get; }
    string OutputPath { get; }
    string Generate(Theme theme, string wallpapersDir);
}
