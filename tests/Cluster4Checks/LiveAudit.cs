using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
public static class LiveAudit
{
    public static async Task Run(string root)
    {
        await using var connection=new SqlConnection("Server=(localdb)\\MSSQLLocalDB;Database=QuanLySanBai;Trusted_Connection=True;TrustServerCertificate=True");
        await connection.OpenAsync();
        foreach(var pair in new[] { ("tables","SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0",48),("triggers","SELECT COUNT(*) FROM sys.triggers WHERE parent_class=1 AND is_ms_shipped=0",13),("procedures","SELECT COUNT(*) FROM sys.procedures WHERE is_ms_shipped=0",3),("views","SELECT COUNT(*) FROM sys.views WHERE is_ms_shipped=0",2),("foreign keys","SELECT COUNT(*) FROM sys.foreign_keys",102) })
        {
            await using var command=new SqlCommand(pair.Item2,connection); var actual=Convert.ToInt32(await command.ExecuteScalarAsync());
            Console.WriteLine($"Live {pair.Item1}: {actual}/{pair.Item3}"); if(actual!=pair.Item3) throw new Exception("Live schema count mismatch: "+pair.Item1);
        }
        var source=File.ReadAllText(Path.Combine(root,"Data","QuanLySanBai_SQLServer.sql"));
        var definitions=Regex.Matches(source,@"EXEC sys\.sp_executesql N'((?:''|[^'])*)';").Select(m=>m.Groups[1].Value.Replace("''","'")).ToList();
        int checkedModules=0;
        foreach(var definition in definitions)
        {
            var name=Regex.Match(definition,@"CREATE\s+(?:PROCEDURE|FUNCTION|TRIGGER|VIEW)\s+dbo\.(\w+)",RegexOptions.IgnoreCase).Groups[1].Value;
            if(name.Length==0) continue;
            await using var command=new SqlCommand("SELECT OBJECT_DEFINITION(OBJECT_ID(@name))",connection); command.Parameters.AddWithValue("@name","dbo."+name);
            var live=(string?)await command.ExecuteScalarAsync() ?? "";
            static string Normalize(string s) { var match=Regex.Match(s,@"CREATE\s+(PROCEDURE|FUNCTION|TRIGGER|VIEW)\b",RegexOptions.IgnoreCase); return Regex.Replace(match.Success ? s[match.Index..] : s,@"\s+"," ").Trim().TrimEnd(';'); }
            if(Normalize(definition)!=Normalize(live)) throw new Exception("Live routine differs from checked-in script: "+name);
            checkedModules++;
        }
        Console.WriteLine($"Live database matches script: {checkedModules} functions/procedures/triggers/views.");
        if(checkedModules!=21) throw new Exception("Incomplete routine audit");
    }
}
