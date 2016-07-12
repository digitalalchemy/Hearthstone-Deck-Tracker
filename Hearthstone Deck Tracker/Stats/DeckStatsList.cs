#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Hearthstone_Deck_Tracker.Utility.Logging;

#endregion

namespace Hearthstone_Deck_Tracker.Stats
{
	public class DeckStatsList
	{
		private static Lazy<DeckStatsList> _instance = new Lazy<DeckStatsList>(Load);

		[XmlArray(ElementName = "DeckStats")]
		[XmlArrayItem(ElementName = "Deck")]
		public List<DeckStats> DeckStats = new List<DeckStats>();

		public static DeckStatsList Instance => _instance.Value;

		private static DeckStatsList Load()
		{
#if(!SQUIRREL)
			SetupDeckStatsFile();
#endif
			var file = Path.Combine(Config.Instance.DataDir, "DeckStats.xml");
			if(!File.Exists(file))
				return new DeckStatsList();
			DeckStatsList instance = null;
			try
			{
				instance = XmlManager<DeckStatsList>.Load(file);
			}
			catch(Exception)
			{
				//failed loading deckstats 
				var corruptedFile = Helper.GetValidFilePath(Config.Instance.DataDir, "DeckStats_corrupted", "xml");
				try
				{
					File.Move(file, corruptedFile);
				}
				catch(Exception)
				{
					throw new Exception(
						"Can not load or move DeckStats.xml file. Please manually delete the file in \"%appdata\\HearthstoneDeckTracker\".");
				}

				//get latest backup file
				var backup =
					new DirectoryInfo(Config.Instance.DataDir).GetFiles("DeckStats_backup*").OrderByDescending(x => x.CreationTime).FirstOrDefault();
				if(backup != null)
				{
					try
					{
						File.Copy(backup.FullName, file);
						instance = XmlManager<DeckStatsList>.Load(file);
					}
					catch(Exception ex)
					{
						throw new Exception(
							"Error restoring DeckStats backup. Please manually rename \"DeckStats_backup.xml\" to \"DeckStats.xml\" in \"%appdata\\HearthstoneDeckTracker\".",
							ex);
					}
				}
				if(instance == null)
					throw new Exception("DeckStats.xml is corrupted.");
			}
			instance.DeckStats = instance.DeckStats.Where(x => x?.Games.Any() ?? false).ToList();
			return instance;
		}

#if(!SQUIRREL)
		internal static void SetupDeckStatsFile()
		{
			if(Config.Instance.SaveDataInAppData == null)
				return;
			var appDataPath = Config.AppDataPath + @"\DeckStats.xml";
			var dataDirPath = Config.Instance.DataDirPath + @"\DeckStats.xml";
			if(Config.Instance.SaveDataInAppData.Value)
			{
				if(File.Exists(dataDirPath))
				{
					if(File.Exists(appDataPath))
					{
						//backup in case the file already exists
						var time = DateTime.Now.ToFileTime();
						File.Move(appDataPath, appDataPath + time);
						Log.Info("Created backups of DeckStats and Games in appdata");
					}
					File.Move(dataDirPath, appDataPath);
					Log.Info("Moved DeckStats to appdata");
				}
			}
			else if(File.Exists(appDataPath))
			{
				if(File.Exists(dataDirPath))
				{
					//backup in case the file already exists
					var time = DateTime.Now.ToFileTime();
					File.Move(dataDirPath, dataDirPath + time);
					Log.Info("Created backups of deckstats and games locally");
				}
				File.Move(appDataPath, dataDirPath);
				Log.Info("Moved DeckStats to local");
			}
		}
#endif


		public static void Save() => XmlManager<DeckStatsList>.Save(Config.Instance.DataDir + "DeckStats.xml", Instance);

		internal static void Reload() => _instance = new Lazy<DeckStatsList>(Load);
	}
}