using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("UserFollows")]
    public class UserFollow
    {
        public long FollowerId { get; set; }

        public long FolloweeId { get; set; }

        public DateTime CreatedAt { get; set; }

        public User? Follower { get; set; }

        public User? Followee { get; set; }
    }
}
