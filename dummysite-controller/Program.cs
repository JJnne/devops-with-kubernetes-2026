using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using k8s;
using k8s.Autorest;
using k8s.Models;

const string Group = "stable.dwk";
const string Version = "v1";
const string Plural = "dummysites";
const int MaxHtmlBytes = 900_000;

var config = KubernetesClientConfiguration.InClusterConfig();
var client = new Kubernetes(config);
var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("dummysite-controller/1.0");

Console.WriteLine("dummysite-controller watching DummySite resources...");

await foreach (var (eventType, raw) in client.CustomObjects.WatchListClusterCustomObjectAsync(
    group: Group, version: Version, plural: Plural))
{
    DummySite? site = null;
    try
    {
        site = ((JsonElement)raw).Deserialize<DummySite>();
        if (eventType == WatchEventType.Added && site is not null)
        {
            await HandleAddedAsync(client, http, site);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to handle event {eventType} for {site?.Metadata?.Name}: {ex.Message}");
    }
}

static async Task HandleAddedAsync(Kubernetes client, HttpClient http, DummySite site)
{
    var name = site.Metadata!.Name;
    var ns = site.Metadata.NamespaceProperty ?? "default";
    var uid = site.Metadata.Uid;
    var websiteUrl = site.Spec?.WebsiteUrl;

    if (string.IsNullOrWhiteSpace(websiteUrl))
    {
        Console.WriteLine($"DummySite {name} has no website_url set, skipping");
        return;
    }

    Console.WriteLine($"Creating resources for DummySite {name} ({websiteUrl}) in namespace {ns}");

    string html;
    try
    {
        html = await http.GetStringAsync(websiteUrl);
        if (html.Length > MaxHtmlBytes)
        {
            html = html[..MaxHtmlBytes];
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to fetch {websiteUrl} for {name}: {ex.Message}");
        html = $"<html><body><p>Failed to fetch {websiteUrl}: {ex.Message}</p></body></html>";
    }

    var ownerReferences = new List<V1OwnerReference>
    {
        new()
        {
            ApiVersion = $"{Group}/{Version}",
            Kind = "DummySite",
            Name = name,
            Uid = uid,
            Controller = true,
            BlockOwnerDeletion = true,
        },
    };

    var appLabel = $"dummysite-{name}";
    var configMapName = $"{appLabel}-html";
    var deploymentName = $"{appLabel}-dep";
    var serviceName = $"{appLabel}-svc";

    var configMap = new V1ConfigMap
    {
        ApiVersion = "v1",
        Kind = "ConfigMap",
        Metadata = new V1ObjectMeta { Name = configMapName, NamespaceProperty = ns, OwnerReferences = ownerReferences },
        Data = new Dictionary<string, string> { ["index.html"] = html },
    };

    var deployment = new V1Deployment
    {
        ApiVersion = "apps/v1",
        Kind = "Deployment",
        Metadata = new V1ObjectMeta { Name = deploymentName, NamespaceProperty = ns, OwnerReferences = ownerReferences },
        Spec = new V1DeploymentSpec
        {
            Replicas = 1,
            Selector = new V1LabelSelector { MatchLabels = new Dictionary<string, string> { ["app"] = appLabel } },
            Template = new V1PodTemplateSpec
            {
                Metadata = new V1ObjectMeta { Labels = new Dictionary<string, string> { ["app"] = appLabel } },
                Spec = new V1PodSpec
                {
                    Containers = new List<V1Container>
                    {
                        new()
                        {
                            Name = "nginx",
                            Image = "nginx:alpine",
                            Ports = new List<V1ContainerPort> { new() { ContainerPort = 80 } },
                            VolumeMounts = new List<V1VolumeMount>
                            {
                                new() { Name = "html", MountPath = "/usr/share/nginx/html" },
                            },
                        },
                    },
                    Volumes = new List<V1Volume>
                    {
                        new()
                        {
                            Name = "html",
                            ConfigMap = new V1ConfigMapVolumeSource { Name = configMapName },
                        },
                    },
                },
            },
        },
    };

    var service = new V1Service
    {
        ApiVersion = "v1",
        Kind = "Service",
        Metadata = new V1ObjectMeta { Name = serviceName, NamespaceProperty = ns, OwnerReferences = ownerReferences },
        Spec = new V1ServiceSpec
        {
            Selector = new Dictionary<string, string> { ["app"] = appLabel },
            Ports = new List<V1ServicePort> { new() { Port = 80, TargetPort = 80 } },
        },
    };

    await CreateIfMissingAsync(name, "ConfigMap", () => client.CoreV1.CreateNamespacedConfigMapAsync(configMap, ns));
    await CreateIfMissingAsync(name, "Deployment", () => client.AppsV1.CreateNamespacedDeploymentAsync(deployment, ns));
    await CreateIfMissingAsync(name, "Service", () => client.CoreV1.CreateNamespacedServiceAsync(service, ns));
}

static async Task CreateIfMissingAsync(string siteName, string kind, Func<Task> create)
{
    try
    {
        await create();
        Console.WriteLine($"Created {kind} for DummySite {siteName}");
    }
    catch (HttpOperationException ex) when (ex.Response.StatusCode == HttpStatusCode.Conflict)
    {
        Console.WriteLine($"{kind} for DummySite {siteName} already exists, skipping");
    }
}

public class DummySite
{
    [JsonPropertyName("apiVersion")]
    public string? ApiVersion { get; set; }

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("metadata")]
    public V1ObjectMeta? Metadata { get; set; }

    [JsonPropertyName("spec")]
    public DummySiteSpec? Spec { get; set; }
}

public class DummySiteSpec
{
    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; set; }
}
