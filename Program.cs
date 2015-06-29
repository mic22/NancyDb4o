using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Idbo;

namespace TreeDb4o
{
    public enum PersonSex { Male, Female, Undefined }

    public class Person
    {
        public String NameSurname { get; set; }
        public DateTime Birthdate { get; set; }
        public DateTime Deathdate { get; set; }
        public PersonSex Sex { get; set; }

        public bool SetDeathdate(DateTime date)
        {
            if(true)
            {
                Deathdate = date;
            }

            return false;
        }

        public Person()
        {
            NameSurname = String.Empty;
            Birthdate = DateTime.MinValue;
            Deathdate = DateTime.MinValue;
            Sex = PersonSex.Undefined;
        }

        public override string ToString()
        {
            var tmp = NameSurname + " " + Sex + "\n* " + Birthdate.Year;
            if (Deathdate != DateTime.MinValue)
                tmp += "\n+ " + Deathdate.Year;
            return tmp;
        }

        static public Person Import(String input)
        {
            var parameters = new Dictionary<string, string>();
            var data = input.Split(';');
            foreach (string item in data)
            {
                var specyficData = item.Split(':');
                parameters[specyficData[0]] = specyficData[1];
            }

            var person = new Person();
            person.NameSurname = parameters["NS"];

            IFormatProvider culture = new System.Globalization.CultureInfo("pl-PL", true);
            if (parameters.ContainsKey("BD"))
            {
                person.Birthdate = DateTime.Parse(parameters["BD"], culture);
            }

            if (parameters.ContainsKey("DD"))
            {
                person.Deathdate = DateTime.Parse(parameters["DD"], culture);
            }

            if (parameters.ContainsKey("S"))
            {
                switch (parameters["S"])
                {
                    case "M": person.Sex = PersonSex.Male; break;
                    case "F": person.Sex = PersonSex.Female; break;
                }
            }
            
            return person;
        }
    }

    public class TreeNode
    {
        //public String Name;
        public Person Person { get; set; }
        public TreeNode Father { get; set; }
        public TreeNode Mother { get; set; }
        public ICollection<TreeNode> Children { get; set; }
        private static ICollection<string> Names = new List<string>();

        public TreeNode(Person Person)
        {
            this.Person = Person;
            Children = new List<TreeNode>();
        }

        private ICollection<TreeNode> GetParents()
        {
            var Parents = new List<TreeNode>();

            foreach (var parent in PParents())
            {
                if(Father != null)
                    Parents.Add(Father);

                if(Mother != null)
                    Parents.Add(Mother);

                Parents.AddRange(parent.PParents());
            }

            return Parents;
        }

        private ICollection<TreeNode> PParents()
        {
            var Parents = new List<TreeNode>();

            if (Father != null)
                Parents.Add(Father);

            if (Mother != null)
                Parents.Add(Mother);

            return Parents;
            
        }

        public static ICollection<TreeNode> GetSharedAccessors(TreeNode first, TreeNode second, IDictionary<string, TreeNode> Source)
        {
            var Shared = new List<TreeNode>();
            var Firsts = first.GetParents();
            var Seconds = second.GetParents();

            foreach (var f in Firsts)
            {
                foreach (var s in Seconds)
                {
                    if (f == s)
                    {
                        Shared.Add(f);
                    }
                }
            }

            return Shared;
        }

        public ICollection<TreeNode> GetInheritants(IDictionary<string, TreeNode> Source)
        {
            var Inheritants = new List<TreeNode>();

            foreach (var child in GetChildren(Source))
            {
                if (child.Person.Deathdate != DateTime.MinValue)
                {
                    Inheritants.Add(child);
                }
                else
                {
                    Inheritants.AddRange(child.GetInheritants(Source));
                }
            }

            return Inheritants;
        }

        public ICollection<TreeNode> GetChildren(IDictionary<string, TreeNode> Source, bool deep = false)
        {
            var Children = new List<TreeNode>();
            foreach (var item in Source)
            {
                var current = item.Value;
                if (((current.Father != null && current.Father.Person.NameSurname == Person.NameSurname) || (current.Mother != null && current.Mother.Person.NameSurname == Person.NameSurname)) && current.IsPossibleToAddAccessor(this))
                {
                    Children.Add(current);
                }
            }

            if (!deep)
            {
                return Children;
            }

            var tmp = new List<TreeNode>();
            foreach (var item in Children)
            {
                tmp.AddRange(item.GetChildren(Source, true));
            }

            Children.AddRange(tmp);

            return Children;
        }

        public static string GetUniqueName(String name)
        {
            if(TreeNode.Names.Contains(name))
            {
                name = name + DateTime.Now.ToString("yyyyMMddHHmmssffff");
            }
            TreeNode.Names.Add(name);
            return name;
        }

        public bool SetParent(TreeNode Parent)
        {
            if (!IsPossibleToAddAccessor(Parent))
            {
                return false;
            }

            switch (Parent.Person.Sex)
            {
                case PersonSex.Male:
                    if(this.Father == null)
                    {
                        this.Father = Parent;
                        return true;
                    }
                    break;
                case PersonSex.Female:
                    if (this.Mother == null)
                    {
                        this.Mother = Parent;
                        return true;
                    }
                    break;
            }

            return false;
        }

        public bool UnsetParent(TreeNode Parent)
        {
            return Parent.UnsetChild(this);
        }

        public bool SetChild(TreeNode Child)
        {
            return Child.SetParent(this);
        }

        public bool UnsetChild(TreeNode Child)
        {
            if (Child.Mother == this)
            {
                Child.Mother = new TreeNode(new Person());
                return true;
            }
            else if(Child.Father == this)
            {
                Child.Father = new TreeNode(new Person());
                return true;
            }

            return false;
        }

        public bool IsPossibleToAddAccessor(TreeNode Accessor)
        {
            var delta = Person.Birthdate.Year - Accessor.Person.Birthdate.Year;
            switch (Accessor.Person.Sex)
            {
                case PersonSex.Male:
                    if (delta >= 12 && delta <= 70)
                    {
                        if(Accessor.Person.Deathdate.Year > 1)
                        {
                            return Person.Birthdate.AddDays(270) <= Accessor.Person.Deathdate;
                        }
                        return true;
                    }
                    break;
                case PersonSex.Female:
                    if (delta >= 10 && delta <= 60)
                    {
                        if (Accessor.Person.Deathdate.Year > 1)
                        {
                            return Person.Birthdate <= Accessor.Person.Deathdate;
                        }
                        return true;
                    }
                    break;
            }
            return false;
        }
    }

    public class Program
    {
        static void Edit(Dictionary<string, TreeNode> Target, string Key, TreeNode ToEdit)
        {
            Target[Key] = ToEdit;
            IdboHelper<Dictionary<string, TreeNode>>.Insert(Target);
        }

        static void Children(Dictionary<string, TreeNode> Target, string Key)
        {
            var node = Target[Key];
            var childs = node.GetChildren(Target, true);
            Console.WriteLine("{0}'s children:", node.Person.NameSurname);
            foreach (var child in childs)
                Console.WriteLine(child.Person);
            Console.WriteLine("--------------------");
        }

        static void SharedParents(Dictionary<string, TreeNode> Target, string Key, string Key1)
        {
            var leszek = Target[Key];
            var natalia = Target[Key1];

            var leszekandnatalia = TreeNode.GetSharedAccessors(leszek, natalia, Target);
            Console.WriteLine("{0}&{1}'s shared accessors: ", leszek.Person.NameSurname, natalia.Person.NameSurname);
            foreach (var item in leszekandnatalia)
            {
                Console.WriteLine(item.Person);
            }
            Console.WriteLine("--------------------");
        }

        static void Inheritants(Dictionary<string, TreeNode> Target, string Key)
        {
            var node = Target[Key];
            Console.WriteLine("{0}'s inheritants:", node.Person.NameSurname);
            var inh = node.GetInheritants(Target);
            foreach (var item in inh)
            {
                Console.WriteLine(item.Person);
            }
            Console.WriteLine("--------------------");
        }

        static void Main(string[] args)
        {
            #region DANE DO BAZY
            /*
            var TreeNodes = new Dictionary<string, TreeNode>();

            TreeNodes.Add("AnnaNode", new TreeNode(Person.Import("NS:Anna;BD:01.01.1935;S:F")));
            TreeNodes.Add("BogdanNode", new TreeNode(Person.Import("NS:Bogdan;BD:01.01.1951;S:M")));
            TreeNodes.Add("CelinaNode", new TreeNode(Person.Import("NS:Celina;BD:01.01.1954;S:F")));
            TreeNodes.Add("DawidNode", new TreeNode(Person.Import("NS:Dawid;BD:01.01.1955;S:M")));
            TreeNodes.Add("ElzbietaNode", new TreeNode(Person.Import("NS:Elżbieta;BD:01.01.1975;S:F")));
            TreeNodes.Add("FranciszekNode", new TreeNode(Person.Import("NS:Franciszek;BD:01.01.1978;S:M")));
            TreeNodes.Add("GrazynaNode", new TreeNode(Person.Import("NS:Grażyna;BD:01.01.1979;S:F")));
            TreeNodes.Add("HenrykNode", new TreeNode(Person.Import("NS:Henryk;BD:01.01.1982;S:M")));
            TreeNodes.Add("IrenaNode", new TreeNode(Person.Import("NS:Irena;BD:01.01.2004;S:F")));
            TreeNodes.Add("JanNode", new TreeNode(Person.Import("NS:Jan;BD:01.01.2006;S:M")));
            TreeNodes.Add("KamilaNode", new TreeNode(Person.Import("NS:Kamila;BD:01.01.2007;S:F")));
            TreeNodes.Add("LeszekNode", new TreeNode(Person.Import("NS:Leszek;BD:01.01.2009;S:M")));
            TreeNodes.Add("MalgorzataNode", new TreeNode(Person.Import("NS:Małgorzata;BD:01.01.2011;S:F")));
            TreeNodes.Add("NataliaNode", new TreeNode(Person.Import("NS:Natalia;BD:01.01.2033;S:F")));

            Console.WriteLine(TreeNodes["CelinaNode"].SetParent(TreeNodes["AnnaNode"]));
            Console.WriteLine(TreeNodes["DawidNode"].SetParent(TreeNodes["AnnaNode"]));
            Console.WriteLine(TreeNodes["ElzbietaNode"].SetParent(TreeNodes["BogdanNode"]));
            Console.WriteLine(TreeNodes["FranciszekNode"].SetParent(TreeNodes["BogdanNode"]));
            Console.WriteLine(TreeNodes["FranciszekNode"].SetParent(TreeNodes["CelinaNode"]));
            Console.WriteLine(TreeNodes["GrazynaNode"].SetParent(TreeNodes["DawidNode"]));
            Console.WriteLine(TreeNodes["HenrykNode"].SetParent(TreeNodes["DawidNode"]));
            Console.WriteLine(TreeNodes["IrenaNode"].SetParent(TreeNodes["ElzbietaNode"]));
            Console.WriteLine(TreeNodes["JanNode"].SetParent(TreeNodes["ElzbietaNode"]));
            Console.WriteLine(TreeNodes["KamilaNode"].SetParent(TreeNodes["FranciszekNode"]));
            Console.WriteLine(TreeNodes["LeszekNode"].SetParent(TreeNodes["GrazynaNode"]));
            Console.WriteLine(TreeNodes["MalgorzataNode"].SetParent(TreeNodes["GrazynaNode"]));
            //Console.WriteLine(TreeNodes["NataliaNode"].SetParent(TreeNodes["HenrykNode"]));
            //Console.WriteLine(TreeNodes["HenrykNode"].SetChild(TreeNodes["NataliaNode"]));
            Console.WriteLine(TreeNodes["LeszekNode"].SetChild(TreeNodes["NataliaNode"]));


            Console.WriteLine(TreeNodes["AnnaNode"].SetParent(TreeNodes["FranciszekNode"]));
            Console.WriteLine(TreeNodes["KamilaNode"].SetParent(TreeNodes["MalgorzataNode"]));
            Console.WriteLine(TreeNodes["LeszekNode"].SetParent(TreeNodes["ElzbietaNode"]));

            //Console.WriteLine(TreeNodes["GrazynaNode"].UnsetChild(TreeNodes["MalgorzataNode"]));
            //Console.WriteLine(TreeNodes["LeszekNode"].UnsetParent(TreeNodes["GrazynaNode"]));

            TreeNodes["GrazynaNode"].Person.SetDeathdate(DateTime.Parse("2013-01-01"));
            TreeNodes["FranciszekNode"].Person.SetDeathdate(DateTime.Parse("2005-01-01"));

            IdboConnection.SetDb();
            IdboHelper<Dictionary<string, TreeNode>>.Insert(TreeNodes);
            */
            #endregion 

            IFormatProvider culture = new System.Globalization.CultureInfo("pl-PL", true);
            IdboConnection.SetDb();

            var TreeNodes = new Dictionary<string, TreeNode>();

            try
            {
                TreeNodes = IdboHelper<Dictionary<string, TreeNode>>.SelectAll()[0];
            }
            catch (Exception e) {  
                IdboHelper<Dictionary<string, TreeNode>>.Insert(TreeNodes);
                TreeNodes = IdboHelper<Dictionary<string, TreeNode>>.SelectAll()[0];
            }
            
            while(true) {
                Console.WriteLine("--# MENU #--");
                Console.WriteLine("0. List people");
                Console.WriteLine("1. Add person");
                Console.WriteLine("2. Edit person");
                Console.WriteLine("3. Remove person");
                Console.WriteLine("4. Add child to parent");
                Console.WriteLine("5. Remove child from parent");
                Console.WriteLine("6. Display all children");
                Console.WriteLine("7. Display shared accessors");
                Console.WriteLine("8. Display inheritants");
                Console.WriteLine("9. Quit");
                
                switch(Console.ReadLine()) {
                    case "0":
                        foreach(var node in TreeNodes) {
                            Console.WriteLine(node.Value.Person);
                        }
                        break;
                    case "1":
                        var NewPerson = new Person();
                        Console.Write("Name: ");
                        NewPerson.NameSurname = Console.ReadLine().ToString();

                        Console.Write("Sex: [m] f");
                        switch (Console.ReadLine()) {
                            case "f": NewPerson.Sex = PersonSex.Female; break;
                            default: NewPerson.Sex = PersonSex.Male; break;
                        }

                        Console.Write("Birth date: ");
                        var tmp = Console.ReadLine();
                        try {
                            NewPerson.Birthdate = DateTime.Parse(tmp, culture);
                        } catch(Exception e) {
                            NewPerson.Birthdate = DateTime.MinValue;
                        }
                        
                        Console.Write("Death date: ");
                        tmp = Console.ReadLine();
                        try {
                            NewPerson.Deathdate = DateTime.Parse(tmp, culture);
                        } catch(Exception e) {
                            NewPerson.Deathdate = DateTime.MinValue;
                        }

                        var Node = new TreeNode(NewPerson);
                        TreeNodes.Add(NewPerson.NameSurname, Node);
                        IdboHelper<Dictionary<string, TreeNode>>.Update(TreeNodes);

                    break;
                    case "2":
                        Console.Write("Person to edit: ");
                        var ToEdit = TreeNodes[Console.ReadLine()];

                        Console.Write("Birth date: {0} ", ToEdit.Person.Birthdate);
                        tmp = Console.ReadLine();
                        try
                        {
                            ToEdit.Person.Birthdate = DateTime.Parse(tmp, culture);
                        }
                        catch (Exception e)
                        {
                            ToEdit.Person.Birthdate = DateTime.MinValue;
                        }

                        Console.Write("Death date: {0} ", ToEdit.Person.Deathdate);
                        tmp = Console.ReadLine();
                        try
                        {
                            ToEdit.Person.Deathdate = DateTime.Parse(tmp, culture);
                        }
                        catch (Exception e)
                        {
                            ToEdit.Person.Deathdate = DateTime.MinValue;
                        }

                        TreeNodes[ToEdit.Person.NameSurname] = ToEdit;
                    break;
                    case "3":
                        Console.Write("Person to delete: ");
                        TreeNodes.Remove(Console.ReadLine());
                    break;
                    case "4":
                        Console.WriteLine("Add relationship");
                        Console.Write("Parent: ");
                        var parent = Console.ReadLine();
                        Console.Write("Child: ");
                        var child = Console.ReadLine();
                        Console.WriteLine(TreeNodes[parent].SetChild(TreeNodes[child]) ? "Child has been added" : "Child has not been added");
                    break;
                    case "5":
                        Console.WriteLine("Remove relationship");
                        Console.Write("Parent: ");
                        parent = Console.ReadLine();
                        foreach (var childd in TreeNodes[parent].Children) {
                            Console.WriteLine(childd.Person);
                        }
                        child = Console.ReadLine();
                        Console.WriteLine(TreeNodes[parent].UnsetChild(TreeNodes[child]));
                    break;
                    case "6":
                        Console.WriteLine("All potomkowie");
                        var nodee = TreeNodes[Console.ReadLine()];
                        var childs = nodee.GetChildren(TreeNodes, true);
                        Console.WriteLine("{0}'s children:", nodee.Person.NameSurname);
                        foreach (var childdd in childs)
                            Console.WriteLine(childdd.Person);
                        break;
                    case "7":
                        Console.WriteLine("Shared accessors");
                        Console.Write("First person: ");
                        var leszek = TreeNodes[Console.ReadLine()];
                        Console.Write("Second person: ");
                        var natalia = TreeNodes[Console.ReadLine()];

                        var leszekandnatalia = TreeNode.GetSharedAccessors(leszek, natalia, TreeNodes);
                        Console.WriteLine("{0}&{1}'s shared accessors: ", leszek.Person.NameSurname, natalia.Person.NameSurname);
                        foreach (var item in leszekandnatalia)
                        {
                            Console.WriteLine(item.Person);
                        }
                        break;
                    case "8":
                        Console.WriteLine("Inheritants");
                        Console.Write("Person: ");
                        var nodeee = TreeNodes[Console.ReadLine()];
                        Console.WriteLine("{0}'s inheritants:", nodeee.Person.NameSurname);
                        var inh = nodeee.GetInheritants(TreeNodes);
                        foreach (var item in inh)
                        {
                            Console.WriteLine(item.Person);
                        }
                        break;
                }
                IdboHelper<Dictionary<string, TreeNode>>.Update(TreeNodes);
            }           

            IdboConnection.CloseDB();
        }
    }
}
