namespace Billing.Model
{
    public class Person
    {        
        public string? Name { get; set; }
        public int Rating { get; set; }
        public UserProfile? Profile { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is Person person &&
                   Name == person.Name &&
                   Rating == person.Rating &&
                   EqualityComparer<UserProfile>.Default.Equals(Profile, person.Profile);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Rating, Profile);
        }
    }
}
