namespace PandaFileImporter;

public static class Extenders
{
    public static bool ImportContains<T>(this List<T> source, T target, string idName = "") where T : class
    {
        idName = idName.ToLower();
        if (string.IsNullOrEmpty(idName))
            idName = $"{typeof(T).Name.ToLower()
                .Replace("import", "")
                .Replace("model", "")}id";

        var props = typeof(T).GetProperties().Where(x => x.Name.ToLower() != idName);

        foreach (var item in source)
            if (props.All(x => x.GetValue(item)?.ToString() == x.GetValue(target)?.ToString())) return true;

        return false;
    }
}