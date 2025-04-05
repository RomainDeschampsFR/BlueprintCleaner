using MelonLoader;

namespace BlueprintCleaner
{
    public class Main : MelonMod
    {
        public static bool vanillaDisplay = false;
        public static List<string> blueprintsRemoved = LoadListFromJson();

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg($"[{Info.Name}] Version {Info.Version} loaded!");
            Settings.OnLoad();
        }

        public static void SaveListToJson(List<string> blueprintsList)
        {
            string path = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Mods", "BlueprintsRemoved_dont_touch_this_file.json");

            string json = "[" + string.Join(",", blueprintsList.ConvertAll(item => $"\"{item}\"")) + "]";

            File.WriteAllText(path, json);
        }


        public static List<string> LoadListFromJson()
        {
            string path = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Mods", "BlueprintsRemoved_dont_touch_this_file.json");

            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);

                List<string> blueprintsList = new List<string>();
                json = json.Trim('[', ']');

                if (!string.IsNullOrEmpty(json))
                {
                    string[] items = json.Split(',');

                    foreach (string item in items)
                    {
                        blueprintsList.Add(item.Trim('"'));
                    }
                }
                return blueprintsList;
            }
            else
            {
                return new List<string>();
            }
        }
    }
}
