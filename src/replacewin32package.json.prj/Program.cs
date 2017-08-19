using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

namespace replacewin32package.json.prj
{
	class Program
	{
		private static readonly Regex _rxX64Target = new Regex(@"win-x64$", RegexOptions.Compiled);

		private static readonly Regex _rxLibraries = new Regex(@"lib_x64.*\.dll$", RegexOptions.Compiled);

		private static void Main(string[] args)
		{
			if(args.Length < 1)
			{
				Console.WriteLine($"Usage: {Path.GetFileName(Assembly.GetExecutingAssembly().Location)} json-file.");
				return;
			}

			var filename = args[0];
			Console.WriteLine($"Processing {filename}...");

			var jobj = Load(filename);
			var x64Libraries = GetX64Libraries(jobj["libraries"]).ToArray();
			var x64Target = GetX64Target(jobj["targets"]);
			ReplaceX64(x64Target, x64Libraries);
			Save(filename, jobj);
		}

		private static JObject Load(string filename)
		{
			using(var s = new StreamReader(filename))
			{
				return JObject.Parse(s.ReadToEnd());
			}
		}

		private static void Save(string filename, JObject jobj)
		{
			using(var sw = new StreamWriter(filename))
			{
				sw.Write(jobj);
			}
		}

		private static JProperty GetX64Target(JToken targets)
		{
			return targets.Cast<JProperty>().FirstOrDefault(target => _rxX64Target.IsMatch(target.Name));
		}

		private static IEnumerable<(string name, string file)> GetX64Libraries(JToken libraries)
		{
			foreach(JProperty library in libraries)
			{
				Console.WriteLine(library.Name);
				var files = library.Value["files"];
				if(files != null)
				{
					foreach(var file in files)
					{
						var filename = file.Value<string>();
						if(_rxLibraries.IsMatch(filename)) yield return (name: library.Name, file: filename);
					}
				}
			}
		}

		private static void ReplaceX64(JProperty x64Target, IReadOnlyCollection<(string name, string file)> x64Libraries)
		{
			foreach(JProperty package in x64Target.Value)
			{
				if(x64Libraries.Any(lib => lib.name == package.Name))
				{
					var libarary = x64Libraries.FirstOrDefault(lib => lib.name == package.Name);
					foreach(JProperty f in package.Value["compile"].ToArray())
					{
						f.Replace(new JProperty(libarary.file, new JObject()));
					}
					foreach(JProperty f in package.Value["runtime"].ToArray())
					{
						f.Replace(new JProperty(libarary.file, new JObject()));
					}
					Console.WriteLine($"Replace {libarary.file}.");
				}
			}
		}
	}
}
