using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Com.Test.Core.Frontend.SingleSignOn
{
    [InitializeOnLoad]
    public class SSOPreProcessBuild : IPreprocessBuildWithReport
    {
        public static bool IsGoogleEnabled = false;
        public static bool IsAppleEnabled = true;
        
        private const string DefineSignInWithGoogle = "#define SIGN_IN_WITH_GOOGLE";
        private const string DefineSignInWithApple = "#define SIGN_IN_WITH_APPLE";
        private const string SsoConstantsHeader = "/Runtime/Plugins/iOS/ICSingleSignOnConstants.h";
        private const string LineComment = "//";

        public int callbackOrder => 0;

        static SSOPreProcessBuild()
        {
            BuildPlayerWindow.RegisterBuildPlayerHandler(ValidateBuild);
        }

        private static void ValidateBuild(BuildPlayerOptions options)
        {
            options.options |= BuildOptions.StrictMode;
            if (options.target == BuildTarget.iOS)
            {
                UpdateProviderDefines(true);
            }

            BuildPipeline.BuildPlayer(options);
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.iOS)
            {
                UpdateProviderDefines(false);
            }
        }

        private static void UpdateProviderDefines(bool startFromUI)
        {
            var constantsHeaderFile = GetPackageRootPath() + SsoConstantsHeader;

            if (!File.Exists(constantsHeaderFile))
            {
                var message =
                    $"File {constantsHeaderFile} not found. Please make sure you build native ios before build unity.";

                ReportError(startFromUI, message);
            }

            SetAppleProviderDefine(constantsHeaderFile, startFromUI);
            SetGoogleProviderDefine(constantsHeaderFile, startFromUI);
        }

        private static string GetPackageRootPath()
        {
            var guid = AssetDatabase.FindAssets($"t:Script {nameof(SSOPreProcessBuild)}");
            var path = AssetDatabase.GUIDToAssetPath(guid[0]);
            return Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, "..");
        }

        private static void SetGoogleProviderDefine(string constantsHeaderFile, bool startFromUi)
        {
            var defineFound = SetProviderDefine(constantsHeaderFile, DefineSignInWithGoogle,
                SSOPreProcessBuild.IsGoogleEnabled);
            if (!defineFound)
            {
                ReportError(startFromUi,
                    $"define {DefineSignInWithGoogle} not found in file {constantsHeaderFile}. Please make sure you build native code in xcode before building the app");
            }
        }

        private static void SetAppleProviderDefine(string constantsHeaderFile, bool startFromUi)
        {
            var defineFound = SetProviderDefine(constantsHeaderFile, DefineSignInWithApple,
                SSOPreProcessBuild.IsAppleEnabled);
            if (!defineFound)
            {
                ReportError(startFromUi,
                    $"define {DefineSignInWithApple} not found in file {constantsHeaderFile}. Please make sure you build native code in xcode before building the app");
            }
        }

        private static bool SetProviderDefine(string serviceMFile, string define, bool isActive)
        {
            var lines = File.ReadAllLines(serviceMFile);
            var writeLines = new string[lines.Length];
            var defineFound = false;
            for (var index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                if (line.Contains(define))
                {
                    if (isActive)
                    {
                        line = define;
                    }
                    else
                    {
                        line = LineComment + define;
                    }

                    defineFound = true;
                }

                writeLines[index] = line;
            }

            File.WriteAllLines(serviceMFile, writeLines);
            return defineFound;
        }

        private static void ReportError(bool startFromUi, string message)
        {
            if (startFromUi)
            {
                throw new BuildPlayerWindow.BuildMethodException(message);
            }
            else
            {
                throw new BuildFailedException(message);
            }
        }
    }
}