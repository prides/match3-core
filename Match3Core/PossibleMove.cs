using System;
using System.Collections.Generic;

namespace Match3Core
{
    public class PossibleMove
    {
        public enum Role
        {
            Key,
            Participant
        }

        public delegate void PossibleMoveEventDelegate(PossibleMove sender);
        public event PossibleMoveEventDelegate OnOver;

        public object tag;

        private List<GemController> participants = new List<GemController>();
        public List<GemController> Participants
        {
            get { return participants; }
        }

        private GemController key = null;
        public GemController Key
        {
            get { return key; }
            private set { key = value; }
        }

        private Position matchablePosition;
        public Position MatchablePosition
        {
            get { return matchablePosition; }
            private set { matchablePosition = value; }
        }

        private Line direction;
        public Line Direction
        {
            get { return direction; }
            private set { direction = value; }
        }

        private bool isValid;
        public bool IsValid
        {
            get { return isValid; }
            private set { isValid = value; }
        }

        private int hash = 0;

        public PossibleMove(List<GemController> participants, GemController key, Position matchablePosition, Line direction)
        {
            Key = key;
            MatchablePosition = matchablePosition;
            Direction = direction;
            hash = MatchUtils.CalculateHash(new int[] { Key.Position.x, Key.Position.y, MatchablePosition.x, MatchablePosition.y });
            Key.AddPossibleMove(this, Role.Key);
            foreach (GemController participant in participants)
            {
                participant.AddPossibleMove(this, Role.Participant);
                Participants.Add(participant);
            }
        }

        internal void RemoveParticipant(GemController participant)
        {
            if (!Participants.Contains(participant))
            {
                return;
            }
            GemController[] relatedParticipants = GetRelatedParticipant(participant);
            Participants.Remove(participant);
            participant.RemovePossibleMove(this, Role.Participant);
            if (relatedParticipants != null)
            {
                foreach (GemController rp in relatedParticipants)
                {
                    Participants.Remove(rp);
                    rp.RemovePossibleMove(this, Role.Participant);
                }
            }
            IsValid = CheckValidity();
            if (!IsValid)
            {
                Over();
            }
        }

        internal void RemoveKey(GemController key)
        {
            if (key != Key)
            {
                return;
            }
            IsValid = false;
            Over();
        }

        internal void Over()
        {
            foreach (GemController participant in Participants)
            {
                participant.RemovePossibleMove(this, Role.Participant);
            }
            Participants.Clear();
            key.RemovePossibleMove(this, Role.Key);
            key = null;
            if (OnOver != null)
            {
                OnOver(this);
            }
        }

        private bool CheckValidity()
        {
            if (key == null)
            {
                return false;
            }
            if (Participants.Count < 2)
            {
                return false;
            }
            int diff = direction == Line.Horizontal ? key.Position.y - Participants[0].Position.y : key.Position.x - Participants[0].Position.x;
            if (Math.Abs(diff) > 1)
            {
                return false;
            }
            List<int> positionList = new List<int>();
            positionList.Add(direction == Line.Horizontal ? key.Position.x : key.Position.y);
            foreach (GemController participant in Participants)
            {
                positionList.Add(direction == Line.Horizontal ? participant.Position.x : participant.Position.y);
            }
            positionList.Sort();
            for (int i = 1; i < positionList.Count; i++)
            {
                if (positionList[i] - positionList[i - 1] != 1)
                {
                    return false;
                }
            }
            return true;
        }

        internal GemController[] GetRelatedParticipant(GemController origin)
        {
            return null;
        }

        public override int GetHashCode()
        {
            return hash;
        }
    }
}
