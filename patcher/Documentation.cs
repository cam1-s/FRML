using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Mono.Cecil;

namespace patcher {
	public static class Documentation {
		public static string dir = "doc";

		public static void SaveIndex() {
			StreamWriter o = File.CreateText(Path.Combine(dir, "index.html"));

			o.WriteLine(htmlHeader);

			o.WriteLine("<h1>FRML Auto-Generated Documentation</h1>");

			foreach (var i in classNames) {
				o.WriteLine(String.Format("<a href={0}>{1}</a><br>", i[1], i[0]));
            }

			o.WriteLine(htmlFooter);

			o.Flush();
			o.Close();
		}

		public static void CreateClass(TypeDefinition type, List<string> callbacks) {
			if (type.Name == "" || type.Name == "<Module>")
				return;

			Directory.CreateDirectory(Path.Combine(dir, "class"));

			string name = type.Name;

			foreach (char c in Path.GetInvalidPathChars()) {
				name = name.Replace(c.ToString(), "_");
            }

			string path = Path.Combine(dir, "class", name + ".html");

			StreamWriter o = File.CreateText(path);

			o.WriteLine(htmlHeader);
			o.WriteLine("<h1>" + type.ToString() + "</h1>");

			o.WriteLine("<h3>Attributes:</h3>");
			o.WriteLine("<p>" + type.Attributes + "</p>");

			o.WriteLine("<h3>BaseType:</h3>");
			o.WriteLine("<p>" + type.BaseType + "</p>");

			o.WriteLine("<h3>Public Methods:</h3>");
			o.WriteLine("<p>");
			string privateMethods = "";
			foreach (var i in type.Methods) {
				string v = i.FullName;
				if (callbacks.Contains(i.Name))
					v += "<span class=green>CALLBACK</span>";
				v += "<br><br>";

				if (i.IsPublic)
					o.WriteLine(v);
				else
					privateMethods += v;
            }
			o.WriteLine("</p>");

			o.WriteLine("<h3>Private Methods: (Use CallMember)</h3>");
			o.WriteLine("<p>" + privateMethods + "</p>");

			o.WriteLine("<h3>Public Fields:</h3>");
			o.WriteLine("<p>");
			string privateFields = "";
			foreach (var i in type.Fields) {
				if (i.IsPublic)
					o.WriteLine(i.FullName + "<br><br>");
				else
					privateFields += i.FullName + "<br><br>";
			}
			o.WriteLine("</p>");

			o.WriteLine("<h3>Private Fields: (Use GetMember/SetMember)</h3>");
			o.WriteLine("<p>" + privateFields + "</p>");

			o.WriteLine("<h3>Properties:</h3>");
			o.WriteLine("<p>");
			foreach (var i in type.Properties) {
				o.WriteLine(i.FullName + "<br><br>");
			}
			o.WriteLine("</p>");

			o.WriteLine(htmlFooter);

			o.Flush();
			o.Close();

			classNames.Add(new string[]{ type.FullName, Path.Combine("class", name + ".html") });
		}

		private static List<string[]> classNames = new List<String[]>();

		private static string htmlHeader =
@"<!DOCTYPE html>
	<html>
		<head>
			<style>
				html {
					font-family: monospace;
				}

				.green {
					color: green;
				}
			</style>
		</head>
		<body>
";
		private static string htmlFooter =
@"		</body>
	</html>
";
	}
}
