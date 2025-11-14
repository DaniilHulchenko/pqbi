namespace PQBI.PQS.CalcEngine
{
    public class IdAndType
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string Type { get; set; }

        public override int GetHashCode()
        {
            if (Id is null)
            {
                return Name.GetHashCode();
            }

            return Id.GetHashCode() ^ Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var result = false;
            if (obj is IdAndType xAxe)
            {
                if (Id is not null)
                {
                    result = Name == xAxe.Name && Id == xAxe.Id;
                }
                else
                {
                    result = Name == xAxe.Name;
                }
            }

            return result;
        }

        public override string ToString()
        {
            return $"{Name}_{Type}_{Id}";
        }
    }
}
