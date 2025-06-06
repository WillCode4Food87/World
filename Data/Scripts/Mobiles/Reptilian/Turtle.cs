using System;
using Server.Mobiles;

namespace Server.Mobiles
{
	[CorpseName( "a turtle corpse" )]
	public class Turtle : BaseCreature
	{
		[Constructable]
		public Turtle() : base( AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.2, 0.4 )
		{
			Name = "a turtle";
			Body = 91;
			BaseSoundID = 0x39D;
			CanSwim = true;

			SetStr( 76, 100 );
			SetDex( 6, 25 );
			SetInt( 11, 20 );

			SetHits( 46, 60 );
			SetStam( 46, 65 );
			SetMana( 0 );

			SetDamage( 5, 15 );

			SetDamageType( ResistanceType.Physical, 100 );

			SetResistance( ResistanceType.Physical, 65, 85 );
			SetResistance( ResistanceType.Fire, 5, 10 );
			SetResistance( ResistanceType.Poison, 5, 10 );

			SetSkill( SkillName.MagicResist, 25.1, 40.0 );
			SetSkill( SkillName.Tactics, 40.1, 60.0 );
			SetSkill( SkillName.FistFighting, 40.1, 60.0 );

			Fame = 700;
			Karma = -700;

			VirtualArmor = 30;
		}

		public override int Meat{ get{ return 1; } }
		public override int Hides{ get{ return 6; } }
		public override int Skeletal{ get{ return Utility.Random(2); } }
		public override SkeletalType SkeletalType{ get{ return SkeletalType.Reptile; } }

		public Turtle(Serial serial) : base(serial)
		{
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);
			writer.Write((int) 0);
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);
			int version = reader.ReadInt();
		}
	}
}