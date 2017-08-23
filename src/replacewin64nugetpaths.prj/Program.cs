using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

namespace ReplaceWin64NugetPaths
{
	class Program
	{
		private static readonly Regex RxLibraries = new Regex(@"lib_x64.*\.dll$", RegexOptions.Compiled);

		private static readonly Regex RxTarget = new Regex(@"^.NETFramework,Version=v(?<fwversion>[\d\.]+)(/(?<platform>.*))?$", RegexOptions.Compiled);

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
			var emptyTarget = GetTarget(jobj["targets"], "");
			Replace(emptyTarget, x64Libraries);
			var anyTarget = GetTarget(jobj["targets"], "win");
			Replace(anyTarget, x64Libraries);
			var x64Target = GetTarget(jobj["targets"], "win-x64");
			Replace(x64Target, x64Libraries);
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

		private static IEnumerable<(string name, string file)> GetX64Libraries(JToken libraries)
		{
			foreach(JProperty library in libraries)
			{
				//Console.WriteLine(library.Name);
				var files = library.Value["files"];
				if(files != null)
				{
					foreach(var file in files)
					{
						var filename = file.Value<string>();
						if(RxLibraries.IsMatch(filename)) yield return (name: library.Name, file: filename);
					}
				}
			}
		}

		private static JProperty GetTarget(JToken targets, string platform)
		{
			foreach(JProperty target in targets)
			{
				var match = RxTarget.Match(target.Name);
				if(match.Success)
				{
					var p = match.Result("${platform}");
					if(p == platform) return target;
				}
			}
			return null;
		}

		private static void Replace(JProperty target, IReadOnlyCollection<(string name, string file)> libraries)
		{
			foreach(JProperty package in target.Value)
			{
				foreach(var library in libraries.Where(lib => lib.name == package.Name))
				{
					foreach(JProperty f in package.Value["compile"].ToArray())
					{
						if(Path.GetFileName(f.Name) == Path.GetFileName(library.file))
						{
							f.Replace(new JProperty(library.file, new JObject()));
						}
					}
					foreach(JProperty f in package.Value["runtime"].ToArray())
					{
						if(Path.GetFileName(f.Name) == Path.GetFileName(library.file))
						{
							f.Replace(new JProperty(library.file, new JObject()));
						}
					}
					Console.WriteLine($"Replace {library.file} for {target.Name}.");
				}
			}
		}
	}
}
