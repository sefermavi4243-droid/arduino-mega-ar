using System.Collections.Generic;
using UnityEngine;

namespace G6
{
    /// <summary>"Parçayı bul" oyunu: rastgele bir parça adı sorulur, kullanıcı nesne üzerinde ona dokunur.</summary>
    public class QuizController
    {
        public const int QuestionCount = 5;

        readonly List<PartView> queue = new List<PartView>();

        public bool Active { get; private set; }
        public int Score { get; private set; }
        public int Asked { get; private set; }
        public PartView Current => queue.Count > 0 ? queue[0] : null;

        public void Begin(PartView[] parts)
        {
            queue.Clear();
            queue.AddRange(parts);
            // Fisher-Yates karıştırma
            for (int i = queue.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (queue[i], queue[j]) = (queue[j], queue[i]);
            }
            if (queue.Count > QuestionCount) queue.RemoveRange(QuestionCount, queue.Count - QuestionCount);
            Score = 0;
            Asked = 0;
            Active = queue.Count > 0;
        }

        /// <returns>Doğru parçaya mı dokunuldu?</returns>
        public bool Answer(PartView tapped)
        {
            bool ok = tapped == Current;
            if (ok) Score++;
            Asked++;
            queue.RemoveAt(0);
            if (queue.Count == 0) Active = false;
            return ok;
        }

        public int Total => Asked + queue.Count;

        public void Stop()
        {
            Active = false;
            queue.Clear();
        }
    }
}
