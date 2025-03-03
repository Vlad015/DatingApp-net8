﻿namespace API.Helpers
{
    public class UserParams:LikesParams
    {
        public string  Gender { get; set; }
        public string? CurrentUsername { get; set; }
        public int MinAge { get; set; } = 18;
        public int MaxAge { get; set; }=80;
        public string OrderBy { get; set; } = "lastActive";
    }

}
