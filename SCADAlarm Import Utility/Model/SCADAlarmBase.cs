using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCADAlarm_Import_Utility.Model
{
    public abstract class SCADAlarmBase
    {
        // Overridden by sub-class
        protected abstract List<LineDelegate> LineDelegates { get; }

        public static List<T> ParseSections<T>(string sectionFile, string sectionDelimiter) where T : new()
        {
            List<List<string>> sections = GetSections(sectionFile, sectionDelimiter);

            // Create a new list of objects of type T
            List<T> newList = new List<T>();

            // Parse the sections
            foreach (List<string> section in sections)
            {
                // Create a new object of type T
                T newObject = new T();

                // Parse the section
                (newObject as SCADAlarmBase).ParseSection(section);

                // Add the new object to the new list of objects
                newList.Add(newObject);
            }

            return newList;
        }

        public void ParseSection(List<string> section)
        {
            foreach (string line in section)
            {
                foreach (LineDelegate lineDelegate in LineDelegates)
                {
                    if (line.StartsWith(lineDelegate.Prefix))
                        try
                        {
                            lineDelegate.Parser.Invoke(line);
                        }
                        catch (Exception e)
                        {
                            throw new Exception(string.Format(
                                "Failed parsing line:\n\n  {0}\n\n{1}",
                                line,
                                e.Message));
                        }
                }
            }
        }

        public delegate void ParseLineDelegate(string line);

        public class LineDelegate
        {
            public ParseLineDelegate Parser { get; set; }
            public string Prefix { get; set; }

            public LineDelegate(ParseLineDelegate parser, string prefix)
            {
                Parser = parser;
                Prefix = prefix;
            }
        }

        public static string GetSectionFile(string[] logicalFiles, string logicalFileHeader)
        {
            foreach (string logicalFile in logicalFiles)
            {
                // If this is the designated logical file ...
                if (logicalFile.StartsWith(logicalFileHeader)) return logicalFile;
            }

            return string.Empty;
        }

        // Get a list of sections from a logical file
        // A logical file is identified by a logicalFileHeader
        // Sections within a logical file are delimited by a sectionDelimiter
        // A section is a list of lines that belong to the same object
        // Think of a section as a database record
        private static List<List<string>> GetSections(string logicalFile, string SectionDelimiter)
        {
            // Create a list of sections
            List<List<string>> sections = new List<List<string>>();

            // Split the logical file into lines
            List<string> lines = logicalFile.Split('\n').ToList<string>();

            // Remove the logical file header info
            lines.RemoveRange(0, 3);

            List<string> section = null;

            // Group the lines into sections
            foreach (string line in lines)
            {
                // If this is the first line of a section ...
                if (line.StartsWith(SectionDelimiter))
                {
                    // Create a new section
                    section = new List<string>();
                }

                // If this is the end of a section ...
                if (line.Length == 0)
                {
                    // Add the new section to the list of sections
                    sections.Add(section);
                }
                else
                {
                    // Add the line to the new section
                    section.Add(line);
                }
            }

            return sections;
        }
    }
}
