using UnityEngine;
using UnityEditor;
using System.IO;

public class BuildScript
{
	private static string apkPath = "Builds/Android/";
	private static string apkName = "Game.apk";

	[MenuItem("Build/Build Android APK")]
	public static void BuildAPK()
	{
		// ビルドの出力ディレクトリを作成
		if (!Directory.Exists(apkPath))
		{
			Directory.CreateDirectory(apkPath);
		}

		// ビルド設定
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = GetScenes();
		buildPlayerOptions.locationPathName = Path.Combine(apkPath, apkName);
		buildPlayerOptions.target = BuildTarget.Android;
		buildPlayerOptions.options = BuildOptions.None;

		// ビルド実行
		BuildPipeline.BuildPlayer(buildPlayerOptions);
	}

	private static string[] GetScenes()
	{
		return System.Array.ConvertAll(EditorBuildSettings.scenes, scene => scene.path);
	}
}
