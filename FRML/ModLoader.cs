using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FRML {
	public static class ModLoader {
		private static StreamWriter log;
		private static Dictionary<UInt64, List<Func<object, object[], int>>> dict;
		private static Dictionary<string, Tuple<string, bool>> textureDict;

		static ModLoader() {
			string[] dir;

			try {
				dir = Directory.GetDirectories("Mods");
			}
			catch(DirectoryNotFoundException) {
				return;
			}

			textureDict = new Dictionary<string, Tuple<string, bool>>();
			dict = new Dictionary<UInt64, List<Func<object, object[], int>>>();
			Application.logMessageReceived += HandleLog;
			log = File.CreateText("FRMLLog.txt");

			ModLoader.Register("LoadingManager", "OnDestroy", OnTextureRefresh);

			foreach (string modPath in dir) {
				try {
					string modName = modPath.Substring(modPath.IndexOf("\\") + 1);
					Assembly mod = Assembly.LoadFrom(Path.Combine(modPath, modName) + ".dll");
					Type entry = mod.GetType(modName);
					entry.GetMethod("Init").Invoke(null, new object[]{});
					Log("Loaded " + modPath + "\n");
				}
				catch (Exception e) {
					Log("Error loading " + modPath + "\n");
					Log(e.ToString() + "\n");
				}
			}
		}

		public static void Log(string text) {
			log.Write(text);
			log.Flush();
		}

		public static void HandleLog(string logString, string stackTrace, LogType type) {
			if (logString.Length > 0) ModLoader.Log(logString + "\n");
			if (stackTrace.Length > 0) ModLoader.Log(stackTrace + "\n");
		}

		public static UInt64 HashCode(string className, string func) {
			byte[] byteContents = Encoding.Unicode.GetBytes(className + "." + func);
			System.Security.Cryptography.SHA256 hash = new System.Security.Cryptography.SHA256CryptoServiceProvider();
			byte[] hashText = hash.ComputeHash(byteContents);
			return BitConverter.ToUInt64(hashText, 0);
		}

		public static void Register(string className, string func, Func<object, object[], int> callback) {
			UInt64 hashCode = HashCode(className, func);

			List<Func<object, object[], int>> list;

			if (!dict.ContainsKey(hashCode)) {
				list = new List<Func<object, object[], int>>();
				dict[hashCode] = list;
			}
			else {
				list = dict[hashCode];
			}

			list.Add(callback);
		}

		public static void Texture(string name, string path) {
			if (!textureDict.ContainsKey(name)) {
				textureDict.Add(name, new Tuple<string, bool>(path, false));
            }
		}

		public static void Call(UInt64 hashCode, object self, object[] param) {
			//var caller = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod();
			//Log(String.Format("Called {0} from {1}.{2}\n", hashCode, caller.DeclaringType, caller.Name));
			
			if (dict.ContainsKey(hashCode)) {
				foreach (Func<object, object[], int> func in dict[hashCode]) {
					try {
						func.Invoke(self, param);
					}
					catch (Exception e) {
						Log(e.ToString() + "\n");
					}
				}
			}
		}

		public static T GetMember<T>(this object obj, string name) {
			FieldInfo field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

			if (field != null)
				return (T)field.GetValue(obj);

			PropertyInfo prop = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic);

			if (prop != null)
				return (T)prop.GetValue(obj, null);

			return default(T);
		}

		public static void SetMember(this object obj, string name, object value) {
			FieldInfo field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

			if (field != null)
				field.SetValue(obj, value);

			PropertyInfo prop = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic);

			if (prop != null)
				prop.SetValue(obj, value, null);
		}

		public static T CallMember<T>(this object obj, string name, params object[] param) {
			MethodInfo m = obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
			return (T)m.Invoke(obj, param);
		}

		public static int OnTextureRefresh(object self, object[] param) {
			Texture[] textures = (Texture[])Resources.FindObjectsOfTypeAll(typeof(Texture));

			foreach (var i in textures) {
				Texture2D t2d = i as Texture2D;

				if (t2d != null) {
					//ModLoader.Log(t2d.name + "\n");

					if (textureDict.ContainsKey(t2d.name)) {
						Tuple<string, bool> t = textureDict[t2d.name];

						if (!t.Item2) {
							textureDict[t2d.name] = new Tuple<string, bool>("", true);

							try {
								t2d.LoadImage(File.ReadAllBytes(t.Item1));
								ModLoader.Log("Patched texture " + t2d.name + "\n");
							}
							catch (Exception e) {
								ModLoader.Log("An error occured while trying to patch texture " + t2d.name + "\n");
								ModLoader.Log(e.ToString() + "\n");
                            }
						}
					}
				}
			}

			return 0;
		}
	}
}
