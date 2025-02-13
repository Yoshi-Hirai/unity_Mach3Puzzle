using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Build.Reporting;

public class BuildScript
{
	private static string apkPath = "Builds/Android/";
	private static string apkName = "Game.apk";

	private static void SaveLog(string log)
	{
		string logFilePath = Path.Combine(apkPath, "build_log.txt");
		File.AppendAllText(logFilePath, log + "\n");
	}

	[MenuItem("Build/Build Android APK")]
	public static void BuildAPK()
	{
		// ビルドの出力ディレクトリを作成
		if (!Directory.Exists(apkPath))
		{
			Directory.CreateDirectory(apkPath);
		}

		// ビルド設定
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
		{
			scenes = GetScenes(),
			locationPathName = Path.Combine(apkPath, apkName),
			target = BuildTarget.Android,
			options = BuildOptions.None
		};

		// **ビルド実行＆結果を取得**
		BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
		BuildSummary summary = report.summary;

		// **エラーの有無を判定**
		if (summary.result == BuildResult.Succeeded)
		{
			Debug.Log("✅ ビルド成功！: " + summary.totalSize + " bytes");
			SaveLog("✅ ビルド成功！");
		}
		else if (summary.result == BuildResult.Failed)
		{
			Debug.LogError("❌ ビルド失敗！エラー内容:");
			SaveLog("❌ ビルド失敗！");
			foreach (var step in report.steps)
			{
				foreach (var message in step.messages)
				{
					if (message.type == LogType.Error)
					{
						Debug.LogError(message.content);
						SaveLog(message.content);
					}
				}
			}
		}
	}

	private static string[] GetScenes()
	{
		return System.Array.ConvertAll(EditorBuildSettings.scenes, scene => scene.path);
	}
}
