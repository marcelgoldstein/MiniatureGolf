namespace MiniatureGolf.DAL.Models
{
    public class TeamPlayer
    {
        public int PlayerId { get; set; }
        public Player Player { get; set; }

        public int TeamId { get; set; }
        public Team Team { get; set; }

        public string Info { get; set; }
    }
}
