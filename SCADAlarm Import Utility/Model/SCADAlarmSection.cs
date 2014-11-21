using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCADAlarm_Import_Utility.Model
{
    abstract public class SCADAlarmSection<T>
    {
        public abstract T ParseSectionHeader(string sectionHeader);
        public abstract void ParseSectionDetails(string line, T thisObject);

        public abstract string LogicalFileHeader { get; }
        public abstract string SectionDelimiter { get; }

        public List<T> ParseSections(string logicalFile)
        {
            List<List<string>> sections = GetSections(logicalFile);

            // Create a new list of objects of type T
            List<T> list = new List<T>();

            foreach (List<string> section in sections) list.Add(ParseSection(section));

            return list;
        }

        private T ParseSection(List<string> section)
        {
            T thisObject;

            // Create a new object of type T
            try
            {
                thisObject = ParseSectionHeader(section[0]);
                section.RemoveAt(0);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Failed parsing header line:\n\n  {0}\n\n{1}", section[0], e.Message));
            }

            foreach (string details in section) 
            try
            {
                ParseSectionDetails(details, thisObject);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Failed parsing detail line:\n\n  {0}\n\n{1}", details, e.Message));
            }

            return thisObject;
        }

        // Get a list of sections from a logical file
        // A logical file is identified by a logicalFileHeader
        // Sections within a logical file are delimited by a sectionDelimiter
        // A section is a list of lines that belong to the same object
        // Think of a section as a database record
        private List<List<string>> GetSections(string logicalFile)
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
