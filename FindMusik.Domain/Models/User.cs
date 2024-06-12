using YoutubeExplode.Search;

namespace FindMusik.Domain.Models;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string LastName { get; set; }
    public string UserName { get; set; }
    public bool isActive { get; set; }
    public List<VideoSearchResult> LastMusikList { get; set; }
}