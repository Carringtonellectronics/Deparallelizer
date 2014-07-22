using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Deparallelizer
{
	class Program
	{
		static void Main(string[] args)
		{
			try
			{
				// Get the input stream
				TextReader reader = null;
				TextWriter writer = Console.Out;

				if (args.Length > 0)
				{
					Stream stream = new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
					reader = new StreamReader(stream);
				}
				else
					reader = Console.In;

				SortedDictionary<int, List<string>> dictionary = new SortedDictionary<int, List<string>>();
				List<string> trailer = new List<string>();
				int nCurrentProcess = 0;
				while (true)
				{
					string sLine = reader.ReadLine();
					if (sLine == null)
						break;

					bool bIncrementProcess = false;

					// Is there a number followed by >
					Regex regex = new Regex("^(?<number>[0-9]+)\\>(?<text>.*)");
					Match match = regex.Match(sLine);
					if (!match.Success)
					{
						// just output it
						if (nCurrentProcess == 0)
							writer.WriteLine(sLine);
						else
							trailer.Add(sLine);
					}
					else
					{
						// No more lead in
						if (nCurrentProcess == 0)
							nCurrentProcess = 1;

						int nProcess = Convert.ToInt32(match.Groups["number"].Value);
						string sText = match.Groups["text"].Value;

						if (nProcess == nCurrentProcess)
						{
							// Should we move to the next process?
							bIncrementProcess = WriteLine(sText, writer);
						}
						else if (nProcess > nCurrentProcess)
						{
							List<String> list = null;
							if (!dictionary.TryGetValue(nProcess, out list))
							{
								list = new List<string>();
								dictionary.Add(nProcess, list);
							}
							list.Add(sText);
						}
						else if (nProcess < nCurrentProcess)
						{
							// Should not get here
							Debug.Assert(false);
						}
					}

					while (bIncrementProcess)
					{
						bIncrementProcess = false;
						nCurrentProcess++;

						// Write existing values from the dictionary
						if (dictionary.ContainsKey(nCurrentProcess))
						{
							List<String> list = dictionary[nCurrentProcess];
							dictionary.Remove(nCurrentProcess);
							bIncrementProcess = WriteLines(list, writer);
						}
					}

				}

				// Purge any leftover data
				foreach (KeyValuePair<int, List<string>> kvp in dictionary)
				{
					int nProcess = kvp.Key;
					Debug.Assert(nProcess > nCurrentProcess);
					nCurrentProcess = nProcess;
					WriteLines(kvp.Value, writer);
				}

				// Write the trailer
				WriteLines(trailer, writer);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}

		}

		static bool WriteLines(List<String> list, TextWriter writer)
		{
			bool bIncrement = false;
			foreach (string s in list)
			{
				bIncrement = WriteLine(s, writer);
			}
			return bIncrement;
		}

		static bool WriteLine(string s, TextWriter writer)
		{
			writer.WriteLine(s);
			if (s.StartsWith("Project not selected to build for this solution configuration"))
				return true;

			Regex regex = new Regex(".*\\-\\s+[0-9]+\\s+error\\(s\\).*");
			Match match = regex.Match(s);
			if (match.Success)
				return true;

			return false;
		}
	}
}
