using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BIO.Framework.Core.Database;
using BIO.Framework.Extensions.Standard.Database.InputDatabase;


namespace BIO.Project.BIORetina
{
    class RetinaDatabaseCreator : IDatabaseCreator<StandardRecord<StandardRecordData>>
    {
        readonly string _databasePath;

        public RetinaDatabaseCreator(string databasePath) {
            _databasePath = databasePath;
        }

        public Database<StandardRecord<StandardRecordData>> createDatabase()
        {
            var database = new Database<StandardRecord<StandardRecordData>>();

            var di = new DirectoryInfo(_databasePath);

            var index_file = _databasePath + @"\index.txt";
            if (!File.Exists(index_file)) {
                throw new Exception("Database index file doesn't exist");
            }

            var files = di.GetFiles("*.pgm");
            foreach (var f in files)
            {
                Console.WriteLine(f);
                var parts = f.Name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                
                string id = parts[0];
                using (StreamReader sr = File.OpenText(index_file))
                {
                    string s = "";
                    while ((s = sr.ReadLine()) != null)
                    {
                        if (s.Contains(parts[0])) {
                            id = s.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries)[0];
                            break;
                        }  
                    }
                }

                //Console.WriteLine(f + ": " + id);

                var bioID = new BiometricID(id, "Retina");
                var data = new StandardRecordData(f.FullName);
                var record = new StandardRecord<StandardRecordData>(f.Name, bioID, data);

                database.addRecord(record);
            }
            
            return database;
        }

    }
}
