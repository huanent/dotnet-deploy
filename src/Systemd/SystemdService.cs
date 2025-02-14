using System.Text;
using DotnetDeploy.Projects;
using DotnetDeploy.Servers;

namespace DotnetDeploy.Systemd;

public class SystemdService : Dictionary<string, Dictionary<string, object>?>
{
   public SystemdService() { }

   public SystemdService(Project project) : base(StringComparer.OrdinalIgnoreCase)
   {

      var remoteAppDirectory = Path.Combine(Server.RootDirectory, project.AssemblyName);
      var remoteAppFile = Path.Combine(remoteAppDirectory, project.AssemblyName);

      Unit = new Dictionary<string, object> {
         {"Description", project.AssemblyName}
      };

      Service = new Dictionary<string, object>{
         {"WorkingDirectory", remoteAppDirectory},
         {"ExecStart", remoteAppFile},
         { "Restart", "always"},
         {"RestartSec", "10"},
         {"KillSignal", "SIGINT"},
         {"SyslogIdentifier", project.AssemblyName},
         {
             "Environment",
             new Dictionary<string,string>{
                 { "ASPNETCORE_ENVIRONMENT","Production"}
             }
         },
      };

      Install = new Dictionary<string, object> {
         {"WantedBy", "multi-user.target"},
      };

      Merge(project.Options?.Systemd);
   }

   public Dictionary<string, object>? Unit { get => base[nameof(Unit)]; set => base[nameof(Unit)] = value; }
   public Dictionary<string, object>? Service { get => base[nameof(Service)]; set => base[nameof(Service)] = value; }
   public Dictionary<string, object>? Install { get => base[nameof(Install)]; set => base[nameof(Install)] = value; }

   public string User { get; set; }

   private void Merge(SystemdService? service)
   {
      if (service == null) return;

      foreach (var item in service)
      {
         this[item.Key] ??= [];
         MergeSection(this[item.Key], service[item.Key]);
      }
   }

   public override string ToString()
   {
      var builder = new StringBuilder();
      foreach (var item in this)
      {
         builder.AppendLine(ToSection(item.Key, item.Value));
      }
      return builder.ToString();
   }

   private static void MergeSection(Dictionary<string, object>? left, Dictionary<string, object>? right)
   {
      if (left == null || right == null) return;

      foreach (var item in right)
      {
         if (item.Value is Dictionary<string, string> rightDictionary)
         {
            if (left[item.Key] == null) left[item.Key] = rightDictionary;
            else if (left[item.Key] is Dictionary<string, string> leftDictionary)
            {
               foreach (var i in rightDictionary)
               {
                  leftDictionary[i.Key] = i.Value;
               }
            }
         }
         else
         {
            left[item.Key] = item.Value;
         }
      }
   }

   private static string ToSection(string name, Dictionary<string, object>? dictionary)
   {
      if (dictionary == null || dictionary.Count == 0) return string.Empty;
      var builder = new StringBuilder();
      builder.AppendLine($"[{name}]");

      foreach (var item in dictionary)
      {
         if (item.Value is Dictionary<string, string> dic)
         {
            foreach (var i in dic)
            {
               var value = $"{i.Key}={i.Value}";
               builder.AppendLine($"{item.Key}={value}");
            }
         }
         else
         {
            builder.AppendLine($"{item.Key}={item.Value}");
         }
      }

      return builder.ToString();
   }
}