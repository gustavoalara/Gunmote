using System;
using System.Collections.Generic;
using System.IO;

namespace WiiTUIO.DeviceUtils
{
    internal class AudioUtil
    {
        private static readonly string[] audioExtensions = { ".wav", ".mp3", ".aac", ".ogg", ".flac" };

        public static bool IsValid(string fileName)
        {
            string baseFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", fileName);

            foreach (string extension in audioExtensions)
            {
                string filePath = baseFilePath + extension;

                if (File.Exists(filePath))
                {
                    if (extension == ".wav" && IsValidFormat(filePath))
                    {
                        return true;
                    }

                    return ConvertToYamahaADPCM(baseFilePath, extension);
                }
            }

            return false;
        }
        /*
        private static bool IsValidFormat(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read)) using (BinaryReader reader = new BinaryReader(fs))
            { 
                byte[] riff = reader.ReadBytes(4);
                if (System.Text.Encoding.ASCII.GetString(riff) != "RIFF")
                    return false;
                reader.ReadInt32();
                
                byte[] wave = reader.ReadBytes(4);
                if (System.Text.Encoding.ASCII.GetString(wave) != "WAVE")
                    return false;
                
                byte[] fmt = reader.ReadBytes(4);
                if (System.Text.Encoding.ASCII.GetString(fmt) != "fmt ")
                    return false;
                reader.ReadInt32();
                
                short formatCode = reader.ReadInt16();
                if (formatCode != 0x0020)
                    return false;

                short channels = reader.ReadInt16();
                if (channels != 1)
                    return false;

                int sampleRate = reader.ReadInt32();
                if (sampleRate != 6000)
                    return false;
                reader.ReadBytes(6);

                short bitsPerSample = reader.ReadInt16();

                return bitsPerSample == 4;
            }
        } */

        private static bool IsValidFormat(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                if (System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF") return false;
                reader.ReadInt32();
                if (System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE") return false;

                if (System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4)) != "fmt ") return false;
                int fmtSize = reader.ReadInt32();

                short formatCode = reader.ReadInt16(); // 1 = PCM
                short channels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                int byteRate = reader.ReadInt32();
                short blockAlign = reader.ReadInt16();
                short bitsPerSample = reader.ReadInt16();

                // Si fmtSize > 16, saltar extra
                if (fmtSize > 16) reader.ReadBytes(fmtSize - 16);

                if (formatCode != 0x0001) return false; // PCM

                if (channels != 1) return false;

                if (sampleRate != 6000) return false;

                if (bitsPerSample != 8) return false;

                // coherencia PCM
                if (blockAlign != 1) return false;          // mono 8-bit => 1
                if (byteRate != 6000) return false;         // 6000 * 1
                return true;
            }
        }


        private static bool ConvertToYamahaADPCM(string baseFilePath, string extension) // Use ffmpeg to convert to valid audio file
        {
            string filePath = baseFilePath + extension;
            string outputPath = baseFilePath + ".wav";
            if (extension == ".wav")
            {
                File.Move(filePath, filePath + ".bak");
                filePath += ".bak";
            }

            if (!Launcher.Launch(null, "ffmpeg", $"-y -i \"{filePath}\" -ar 6000 -ac 1 -c:a pcm_u8 -af \"volume=12dB\" \"{outputPath}\" -hide_banner", null))
            {
                if (extension == ".wav") File.Move(filePath, baseFilePath + extension);
                return false;
            }

            return File.Exists(outputPath) && new FileInfo(outputPath).Length > 0;
        }
    }
}
