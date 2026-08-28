using iReverse_Unisoc_Ultimate.UniFlash.Worker;
using System;
using System.IO;

namespace iReverse_Unisoc_Ultimate
{
    namespace UniFlash
    {
        internal static class UniCommandBuilder
        {
            public static string Build(params string[] operations)
            {
                var cmd = new System.Text.StringBuilder();
                cmd.Append("-progress -wait 5 -timeout ");
                cmd.Append(uni.Timeout.Replace(" ", ""));

                if (uni.isRSAExploit && !string.IsNullOrEmpty(uni.exploit))
                {
                    cmd.Append(" -exploit ");
                    cmd.Append(uni.exploit);
                }

                if (File.Exists(uni.fdl2_location))
                {
                    cmd.Append(" -fdl \"");
                    cmd.Append(uni.fdl1_location);
                    cmd.Append("\" ");
                    cmd.Append(uni.fdl1_addr);
                    cmd.Append(" -fdl \"");
                    cmd.Append(uni.fdl2_location);
                    cmd.Append("\" ");
                    cmd.Append(uni.fdl2_addr);
                    cmd.Append(" -exec");
                }
                else
                {
                    cmd.Append(" -fdl \"");
                    cmd.Append(uni.fdl1_location);
                    cmd.Append("\" ");
                    cmd.Append(uni.fdl1_addr);
                    cmd.Append(" -exec");
                }

                foreach (string op in operations)
                {
                    if (!string.IsNullOrEmpty(op))
                    {
                        cmd.Append(" ");
                        cmd.Append(op);
                    }
                }

                return cmd.ToString();
            }

            public static string BuildWithRepartition(string xmlPath, params string[] operations)
            {
                var ops = new System.Collections.Generic.List<string>(operations);
                if (!string.IsNullOrEmpty(xmlPath) && File.Exists(xmlPath))
                {
                    ops.Insert(0, "-repartition \"" + xmlPath + "\"");
                }
                return Build(ops.ToArray());
            }

            public static string BuildFlash(params string[] partitionOperations)
            {
                var ops = new System.Collections.Generic.List<string>();
                foreach (string op in partitionOperations)
                {
                    if (!string.IsNullOrEmpty(op))
                        ops.Add(op);
                }
                return Build(ops.ToArray());
            }

            public static string BuildRead(string saveFolder, bool readFromList = false)
            {
                var ops = new System.Collections.Generic.List<string>();
                if (readFromList)
                {
                    ops.Add("-rsize");
                }
                else
                {
                    ops.Add("-r");
                }
                return Build(ops.ToArray());
            }

            public static string BuildErase(params string[] partitions)
            {
                var ops = new System.Collections.Generic.List<string>();
                foreach (string p in partitions)
                {
                    if (!string.IsNullOrEmpty(p))
                        ops.Add("-e " + p);
                }
                return Build(ops.ToArray());
            }

            public static string BuildIdentify()
            {
                return Build("-gpt", "-get_deviceinfo \"" + uni.Temp + "\\boot.img\"");
            }
        }
    }
}
