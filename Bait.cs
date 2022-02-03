using DiskCardGame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VoiceCommandMod
{
    public class Bait : SpecialCardBehaviour
    {
        public override bool RespondsToDie(bool wasSacrifice, PlayableCard killer)
        {
            return true;
        }

        public override IEnumerator OnDie(bool wasSacrifice, PlayableCard killer)
        {
			yield return new WaitForSeconds(0.2f);
			CardInfo shark = CardLoader.GetCardByName("Shark");
			yield return Singleton<BoardManager>.Instance.CreateCardInSlot(shark, PlayableCard.Slot, 0.1f, true);
			yield return new WaitForSeconds(0.25f);
			yield return new WaitForSeconds(0.1f);
			yield break;
		}
    }
}
