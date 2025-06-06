using System;
using System.Collections;
using Server;
using Server.Targeting;
using Server.Network;
using Server.Mobiles;
using Server.Spells;

namespace Server.SkillHandlers
{
	public class Taming
	{
		private static Hashtable m_BeingTamed = new Hashtable();

		public static void Initialize()
		{
			SkillInfo.Table[(int)SkillName.Taming].Callback = new SkillUseCallback( OnUse );
		}

		private static bool m_DisableMessage;

		public static bool DisableMessage
		{
			get{ return m_DisableMessage; }
			set{ m_DisableMessage = value; }
		}

		public static TimeSpan OnUse( Mobile m )
		{
			m.RevealingAction();

			m.Target = new InternalTarget();
			m.RevealingAction();

			if ( !m_DisableMessage )
				m.SendLocalizedMessage( 502789 ); // Tame which animal?

			return TimeSpan.FromHours( 6.0 );
		}

		public static bool CheckMastery( Mobile tamer, BaseCreature creature )
		{
			BaseCreature familiar = (BaseCreature)Spells.Necromancy.SummonFamiliarSpell.Table[tamer];

			if ( familiar != null && !familiar.Deleted && familiar is DarkWolfFamiliar )
			{
				if ( creature is Worg || creature is WolfDire || creature is GreyWolf || creature is TimberWolf || creature is WhiteWolf || creature is MysticalFox )
					return true;
			}

			return false;
		}

		public static bool MustBeSubdued( BaseCreature bc )
		{
            if (bc.Owners.Count > 0) { return false; } //Checks to see if the animal has been tamed before
			return bc.SubdueBeforeTame && (bc.Hits > (bc.HitsMax / 10));
		}

		public static void ScaleStats( BaseCreature bc, double scalar )
		{
			if ( bc.RawStr > 0 )
				bc.RawStr = (int)Math.Max( 1, bc.RawStr * scalar );

			if ( bc.RawDex > 0 )
				bc.RawDex = (int)Math.Max( 1, bc.RawDex * scalar );

			if ( bc.RawInt > 0 )
				bc.RawInt = (int)Math.Max( 1, bc.RawInt * scalar );

			if ( bc.HitsMaxSeed > 0 )
			{
				bc.HitsMaxSeed = (int)Math.Max( 1, bc.HitsMaxSeed * scalar );
				bc.Hits = bc.Hits;
				}

			if ( bc.StamMaxSeed > 0 )
			{
				bc.StamMaxSeed = (int)Math.Max( 1, bc.StamMaxSeed * scalar );
				bc.Stam = bc.Stam;
			}
		}

		public static void ScaleSkills( BaseCreature bc, double scalar )
		{
			ScaleSkills( bc, scalar, scalar );
		}

		public static void ScaleSkills( BaseCreature bc, double scalar, double capScalar )
		{
			for ( int i = 0; i < bc.Skills.Length; ++i )
			{
				bc.Skills[i].Base *= scalar;

				bc.Skills[i].Cap = Math.Max( 100.0, bc.Skills[i].Cap * capScalar );

				if ( bc.Skills[i].Base > bc.Skills[i].Cap )
				{
					bc.Skills[i].Cap = bc.Skills[i].Base;
				}
			}
		}

		private class InternalTarget : Target
		{
			private bool m_SetSkillTime = true;

			public InternalTarget() :  base ( 2, false, TargetFlags.None )
			{
			}

			protected override void OnTargetFinish( Mobile from )
			{
				if ( m_SetSkillTime )
					from.NextSkillTime = DateTime.Now;
			}

			public virtual void ResetPacify( object obj )
			{
				if( obj is BaseCreature )
				{
					((BaseCreature)obj).BardPacified = true;
				}
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				from.RevealingAction();

				if ( targeted is Mobile )
				{
					if ( targeted is BaseCreature )
					{
						BaseCreature creature = (BaseCreature)targeted;

						if ( !creature.Tamable )
						{
							creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1049655, from.NetState ); // That creature cannot be tamed.
						}
						else if ( creature.Controlled )
						{
							creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502804, from.NetState ); // That animal looks tame already.
						}
						else if ( from.Female && !creature.AllowFemaleTamer )
						{
							creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1049653, from.NetState ); // That creature can only be tamed by males.
						}
						else if ( !from.Female && !creature.AllowMaleTamer )
						{
							creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1049652, from.NetState ); // That creature can only be tamed by females.
						}
						else if ( from.Followers + creature.ControlSlots > from.FollowersMax )
						{
							from.SendLocalizedMessage( 1049611 ); // You have too many followers to tame that creature.
						}
						else if ( creature.Owners.Count >= BaseCreature.MaxOwners && !creature.Owners.Contains( from ) )
						{
							creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1005615, from.NetState ); // This animal has had too many owners and is too upset for you to tame.
						}
						else if ( MustBeSubdued( creature ) )
						{
							creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1054025, from.NetState ); // You must subdue this creature before you can tame it!
						}
						else if ( CheckMastery( from, creature ) || from.Skills[SkillName.Taming].Value >= creature.MinTameSkill )
						{
							if ( m_BeingTamed.Contains( targeted ) )
							{
								creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502802, from.NetState ); // Someone else is already taming this.
							}
							else if ( creature.CanAngerOnTame && 0.95 >= Utility.RandomDouble() )
							{
								creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502805, from.NetState ); // You seem to anger the beast!
								creature.PlaySound( creature.GetAngerSound() );
								creature.Direction = creature.GetDirectionTo( from );

								if( creature.BardPacified && Utility.RandomDouble() > .24)
								{
									Timer.DelayCall( TimeSpan.FromSeconds( 2.0 ), new TimerStateCallback( ResetPacify ), creature );
								}
								else
								{
									creature.BardEndTime = DateTime.Now;
								}
		
								creature.BardPacified = false;

								creature.Move( creature.Direction );

								if ( from is PlayerMobile )
									creature.Combatant = from;
							}
							else
							{
								m_BeingTamed[targeted] = from;

								from.LocalOverheadMessage( MessageType.Emote, 0x59, 1010597 ); // You start to tame the creature.
								from.NonlocalOverheadMessage( MessageType.Emote, 0x59, 1010598 ); // *begins taming a creature.*

								new InternalTimer( from, creature, Utility.Random( 3, 2 ) ).Start();

								m_SetSkillTime = false;
							}
						}
						else
						{
							creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502806, from.NetState ); // You have no chance of taming this creature.
						}
					}
					else
					{
						((Mobile)targeted).PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502469, from.NetState ); // That being cannot be tamed.
					}
				}
				else
				{
					from.SendLocalizedMessage( 502801 ); // You can't tame that!
				}
			}

			private class InternalTimer : Timer
			{
				private Mobile m_Tamer;
				private BaseCreature m_Creature;
				private int m_MaxCount;
				private int m_Count;
				private bool m_Paralyzed;
				private DateTime m_StartTime;

				public InternalTimer( Mobile tamer, BaseCreature creature, int count ) : base( TimeSpan.FromSeconds( 3.0 ), TimeSpan.FromSeconds( 3.0 ), count )
				{
					m_Tamer = tamer;
					m_Creature = creature;
					m_MaxCount = count;
					m_Paralyzed = creature.Paralyzed;
					m_StartTime = DateTime.Now;
					Priority = TimerPriority.TwoFiftyMS;
				}

				protected override void OnTick()
				{
					m_Count++;

					DamageEntry de = m_Creature.FindMostRecentDamageEntry( false );
					bool alreadyOwned = m_Creature.Owners.Contains( m_Tamer );

					if ( !m_Tamer.InRange( m_Creature, 6 ) )
					{
						m_BeingTamed.Remove( m_Creature );
						m_Tamer.NextSkillTime = DateTime.Now;
						m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502795, m_Tamer.NetState ); // You are too far away to continue taming.
						Stop();
					}
					else if ( !m_Tamer.CheckAlive() )
					{
						m_BeingTamed.Remove( m_Creature );
						m_Tamer.NextSkillTime = DateTime.Now;
						m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502796, m_Tamer.NetState ); // You are dead, and cannot continue taming.
						Stop();
					}
					else if ( !m_Tamer.CanSee( m_Creature ) || !m_Tamer.InLOS( m_Creature ) || !CanPath() )
					{
						m_BeingTamed.Remove( m_Creature );
						m_Tamer.NextSkillTime = DateTime.Now;
						m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1049654, m_Tamer.NetState ); // You do not have a clear path to the animal you are taming, and must cease your attempt.
						Stop();
					}
					else if ( !m_Creature.Tamable )
					{
						m_BeingTamed.Remove( m_Creature );
						m_Tamer.NextSkillTime = DateTime.Now;
						m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1049655, m_Tamer.NetState ); // That creature cannot be tamed.
						Stop();
					}
					else if ( m_Creature.Controlled )
					{
						m_BeingTamed.Remove( m_Creature );
						m_Tamer.NextSkillTime = DateTime.Now;
						m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502804, m_Tamer.NetState ); // That animal looks tame already.
						Stop();
					}
					else if ( m_Creature.Owners.Count >= BaseCreature.MaxOwners && !m_Creature.Owners.Contains( m_Tamer ) )
					{
						m_BeingTamed.Remove( m_Creature );
						m_Tamer.NextSkillTime = DateTime.Now;
						m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1005615, m_Tamer.NetState ); // This animal has had too many owners and is too upset for you to tame.
						Stop();
					}
					else if ( MustBeSubdued( m_Creature ) )
					{
						m_BeingTamed.Remove( m_Creature );
						m_Tamer.NextSkillTime = DateTime.Now;
						m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 1054025, m_Tamer.NetState ); // You must subdue this creature before you can tame it!
						Stop();
					}
					else if ( de != null && de.LastDamage > m_StartTime )
					{
						m_BeingTamed.Remove( m_Creature );
						m_Tamer.NextSkillTime = DateTime.Now;
						m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502794, m_Tamer.NetState ); // The animal is too angry to continue taming.
						Stop();
					}
					else if ( m_Count < m_MaxCount )
					{
						m_Tamer.RevealingAction();

						switch ( Utility.Random( 32 ) )
						{
							case 0: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Easy...easy..."); break;
							case 1: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Don't be afraid..."); break;
							case 2: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "I won't hurt you..."); break;
							case 3: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "See? Nothing to be afraid of..."); break;
							case 4: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Nice and easy..."); break;
							case 5: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "We will be great friends..."); break;
							case 6: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "I always wanted a companion like you..."); break;
							case 7: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "I'll protect you from the dangers of this world..."); break;
							case 8: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "We could accomplish much by joining forces..."); break;
							case 9: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "You will make many friends with me..."); break;
							case 10: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "We will venture far into the unknown together..."); break;
							case 11: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Easy now, friend... I mean you no harm..."); break;
							case 12: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Come with me and I'll show you a better life..."); break;
							case 13: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Steady. You are safe with me..."); break;
							case 14: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "You are so fierce. I'll always respect you..."); break;
							case 15: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Come on now, you're not really going to bite me, are you?"); break;
							case 16: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "There are no chains here, only trust..."); break;
							case 17: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "You've been alone a long time, haven't you?"); break;
							case 18: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "You're fierce, but I see wisdom in your eyes..."); break;
							case 19: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "I promise, you'll roam far and feast well..."); break;
							case 20: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "There's a place for you by my side, if you'll have it..."); break;
							case 21: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Let go of fear. I'm not your enemy..."); break;
							case 22: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "I've come a long way for you..."); break;
							case 23: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "We can be stronger together..."); break;
							case 24: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "You've been through a lot, haven't you? I can feel it..."); break;
							case 25: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Shhh... It's alright now. I'm here..."); break;
							case 26: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "The stars told me I'd meet you..."); break;
							case 27: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Well, you're not the scariest thing I've tried to pet..."); break;
							case 28: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Just pretend I'm competent and this'll go smoothly..."); break;
							case 29: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "If you bite me I'll bite you. Be warned!"); break;
							case 30: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "You don't trust easily. I understand..."); break;
							case 31: m_Tamer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "They tried to chain you... I won't..."); break;
						}

						if ( !alreadyOwned ) // Passively check druidism for gain
							m_Tamer.CheckTargetSkill( SkillName.Druidism, m_Creature, 0.0, 125.0 );

						if ( m_Creature.Paralyzed )
							m_Paralyzed = true;
					}
					else
					{
						m_Tamer.RevealingAction();
						m_Tamer.NextSkillTime = DateTime.Now;
						m_BeingTamed.Remove( m_Creature );

						if ( m_Creature.Paralyzed )
							m_Paralyzed = true;

						if ( !alreadyOwned ) // Passively check druidism for gain
							m_Tamer.CheckTargetSkill( SkillName.Druidism, m_Creature, 0.0, 125.0 );

						double minSkill = m_Creature.MinTameSkill + (m_Creature.Owners.Count * 6.0);

						if ( minSkill > -24.9 && CheckMastery( m_Tamer, m_Creature ) )
							minSkill = -24.9; // 50% at 0.0?

						minSkill += 24.9;

						double mod = m_Tamer.Skills[SkillName.Druidism].Value / 5;

						if ( CheckMastery( m_Tamer, m_Creature ) || alreadyOwned || m_Tamer.CheckTargetSkill( SkillName.Taming, m_Creature, (minSkill-25.0-mod), (minSkill+25.0) ) )
						{
							if ( m_Creature.Owners.Count == 0 ) // First tame
							{
								if ( m_Paralyzed )
									ScaleSkills( m_Creature, 0.86 ); // 86% of original skills if they were paralyzed during the taming
								else
									ScaleSkills( m_Creature, 0.90 ); // 90% of original skills

								if ( m_Creature.StatLossAfterTame )
									ScaleStats( m_Creature, 0.50 );
							}

							if ( alreadyOwned )
							{
								m_Tamer.SendLocalizedMessage( 502797 ); // That wasn't even challenging.
							}
							else
							{
								m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502799, m_Tamer.NetState ); // It seems to accept you as master.
								m_Creature.Owners.Add( m_Tamer );
							}

							m_Creature.SetControlMaster( m_Tamer );
							m_Creature.Fame = 0;
							m_Creature.Karma = 0;
							m_Creature.RangeHome = -1;
							m_Creature.Home = new Point3D(0, 0, 0);
							m_Creature.FightMode = FightMode.Aggressor;
							m_Creature.IsBonded = false;
                            #region KoperPets
                            Server.Custom.KoperPets.KoperPetManager.RegisterPet(m_Creature);
                            #endregion
                        }
                        else
						{
							m_Creature.PrivateOverheadMessage( MessageType.Regular, 0x3B2, 502798, m_Tamer.NetState ); // You fail to tame the creature.
						}
					}
				}

				private bool CanPath()
				{
					IPoint3D p = m_Tamer as IPoint3D;

					if ( p == null )
						return false;

					if( m_Creature.InRange( new Point3D( p ), 2 ) )
						return true;

					MovementPath path = new MovementPath( m_Creature, new Point3D( p ) );
					return path.Success;
				}
			}
		}
	}
}