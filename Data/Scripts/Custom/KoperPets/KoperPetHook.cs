using System;
using System.IO;
using Server;
using Server.Network;
using Server.Mobiles;

namespace Server.Custom.KoperPets
{
    public class KoperPetTamingHook
    {
        public static void Initialize()
        {
            Console.WriteLine("KoperPetHook initialized"); // DEBUG
            EventSink.WorldLoad += OnServerStart;
            EventSink.WorldSave += new WorldSaveEventHandler(OnWorldSave);
            EventSink.Shutdown += OnServerShutdown;

            OnServerStart();
        }
        private static void OnServerStart()
        {
            if (File.Exists(KoperPetManager.saveFilePath))
            {
                KoperPetManager.LoadAllPets();
                KoperPetNursery.LoadNurseryData(); // DEBUG
                return;
            }

            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is BaseCreature)
                {
                    BaseCreature pet = (BaseCreature)m;

                    if (pet.Controlled && pet.ControlMaster is PlayerMobile)
                    {
                        if (KoperPetManager.GetPetData(pet) == null)
                        {
                            KoperPetManager.RegisterPet(pet);
                        }
                    }
                }
            }
        }

        // Save pets when shutting down
        private static void OnServerShutdown(ShutdownEventArgs e)
        {
            KoperPetNursery.SaveNurseryData();
            KoperPetManager.SaveAllPets();
        }

        private static void OnWorldSave(WorldSaveEventArgs e)
        {
            KoperPetNursery.SaveNurseryData();
            KoperPetManager.SaveAllPets();
        }
    }
}



