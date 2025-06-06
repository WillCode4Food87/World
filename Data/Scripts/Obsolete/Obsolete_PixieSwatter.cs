using System;
using Server;

namespace Server.Items
{
	public class PixieSwatter : Scepter
	{
		public override int InitMinHits{ get{ return 80; } }
		public override int InitMaxHits{ get{ return 160; } }

		public override int LabelNumber{ get{ return 1070854; } } // Pixie Swatter

		[Constructable]
		public PixieSwatter()
		{
			Hue = 0x8A;
			WeaponAttributes.HitPoisonArea = 75;
			Attributes.WeaponSpeed = 30;
            
			WeaponAttributes.UseBestSkill = 1;
			WeaponAttributes.ResistFireBonus = 12;
			WeaponAttributes.ResistEnergyBonus = 12;

			Slayer = SlayerName.Fey;
		}

        public override void AddNameProperties(ObjectPropertyList list)
		{
            base.AddNameProperties(list);
			list.Add( 1070722, "Artefact");
        }

		public override void GetDamageTypes( Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct )
		{
			fire = 100;

			cold = pois = phys = nrgy = chaos = direct = 0;
		}

		public PixieSwatter( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 );
		}
		
		private void Cleanup( object state ){ Item item = new Artifact_PixieSwatter(); Server.Misc.Cleanup.DoCleanup( (Item)state, item ); }

public override void Deserialize(GenericReader reader)
		{
			base.Deserialize( reader ); Timer.DelayCall( TimeSpan.FromSeconds( 1.0 ), new TimerStateCallback( Cleanup ), this );

			int version = reader.ReadInt();
		}
	}
}