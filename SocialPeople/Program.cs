using AForge.Neuro;
using AForge.Neuro.Learning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SocialPeople
{
    class Program
    {
        static ActivationNetwork network;

        static void Main(string[] args)
        {
            var jStatuses = new TwitterParser("Jenna_Marbles",10).GetStatuses().Aggregate((i, k) => i + " " + k);
            var tStatuses = new TwitterParser("taylorswift13", 10).GetStatuses().Aggregate((i, k) => i + " " + k);

            //            var split = RemoveStopWords(@"Got home a few hours ago and already had to take Kermit to the vet. He's okay don't worry, it'll be in the vlog tonight. But poor guy 😭
            //Happy Thanksgiving! Spending the day cooking and laughing and hugging dogs and being infinitely grateful. 🐩🐩🐩🐩🐩🐩
            //Video's up: Oops I'm In Tokyo https://www.youtube.com/watch?v=JW70oTiQbm8&feature=youtu.be …
            //I'm home now though, yayyyy happy thanksgiving!");

            var jSplit = RemoveStopWords(jStatuses);
            var tSplit = RemoveStopWords(tStatuses);
            var jNgrams = GetNGrams(jSplit, 3);
            var tNgrams = GetNGrams(tSplit, 3);
            var jDict = GetDictionary(jNgrams);
            var tDict = GetDictionary(tNgrams);

            var combined = GetCombinedPhrases(jDict, tDict);

            var j = new Dictionary<string, double>();

            j.Add("jenna", 1);
            j.Add("tswift", 0);

            var t = new Dictionary<string, double>();

            t.Add("jenna", 0);
            t.Add("tswift", 1);

            network = CreateNetwork(combined.Count, t.Count);           
            
            var err = TrainNeuralNetwork(combined, new Dictionary<string, double>[2] { jDict, tDict }, new Dictionary<string, double>[2] { j, t });

            var kStatuses = new TwitterParser("k1mberrito", 10).GetStatuses().Aggregate((i, k) => i + " " + k);
            var kSplit = RemoveStopWords(kStatuses);
            var kNgrams = GetNGrams(kSplit, 3);
            var kDict = GetDictionary(kNgrams);

            var output = FireNetwork(combined, kDict);

            if (true) ;
        }

        const int WORD_COUNT_THRESHOLD = 1;

        static List<string> GetCombinedPhrases(params Dictionary<string, double>[] dicts)
        {
            var combined = new Dictionary<string, double>();
            foreach (var dict in dicts) { 
                foreach (var x in dict)
                {
                    if (combined.ContainsKey(x.Key))
                    {
                        combined[x.Key] += x.Value;
                    }
                    else
                    {
                        combined[x.Key] = x.Value;
                    }
                }
            }

            var result = new List<string>();

            foreach(var kvp in combined)
            {
                if(kvp.Value > WORD_COUNT_THRESHOLD)
                {
                    result.Add(kvp.Key);
                }
            }

            return result;
        }

        static ActivationNetwork CreateNetwork(int inputs, int outputs)
        {
            return new ActivationNetwork(
                new SigmoidFunction(1),
                inputs, 
                (inputs + outputs) / 2 + 1, 
                outputs);
        }

        static string[] STOP_WORDS = new string[] { "a", "an", "and", "are", "as", "at", "be", "by", "for", "from", "has", "he", "in", "is", "it", "its", "of", "on", "she", "so", "that", "the", "to", "was", "were", "will", "with"};
        static string[] RemoveStopWords(string input)
        {
            string spacesAdded = Regex.Replace(input, @"([^a-zA-Z0-9@#'\s])", " $0");
            spacesAdded = spacesAdded.Replace("'", "");
            foreach (var thisStop in STOP_WORDS)
            {
                spacesAdded = Regex.Replace(spacesAdded, @"(\b\s*" + thisStop + @"\s*\b)", " ", RegexOptions.IgnoreCase);
            }
            return spacesAdded.Split(' ');
        }

        static string[] GetNGrams(string[] input, int n)
        {
            var ngrams = new List<string>();
            for(int i = 0; i < input.Length - (n-1); i++)
            {
                string temp = "";
                for (int k = 0; k < n; k++)
                {
                    temp += (k == 0 ? "" : " ") + input[i + k];
                    ngrams.Add(temp);
                }
            }

            return ngrams.ToArray();
        }

        static Dictionary<string,double> GetDictionary(string [] input)
        {
            var dict = new Dictionary<string, double>();

            foreach (var value in input)
            {
                if (dict.ContainsKey(value))
                    dict[value]++;
                else
                    dict[value] = 1;
            }

            return dict;
        }

        static double TrainNeuralNetwork(List<string> words, Dictionary<string,double> [] userWordsDicts, Dictionary<string,double>[] users)
        {
            var tempIn = new double[userWordsDicts.Length][];
            int j = 0;

            foreach (var userWordsDict in userWordsDicts)
            {
                var thisList = new List<double>();
                
                foreach (var word in words)
                {
                    if (userWordsDict.ContainsKey(word))
                        thisList.Add(userWordsDict[word]);
                    else
                        thisList.Add(0);
                }

                tempIn[j] = thisList.ToArray();
                j++;
            }

            j = 0;
            foreach (var inputList in tempIn)
            {
                var input = inputList.ToArray();
                var max = Math.Max(input.Max(), 1);                
                tempIn[j] = input.Select(i => i / max).ToArray();
                j++;
            }

            double[][] output = new double[users.Length][];

            j = 0;
            foreach (var userSet in users)
            {
                output[j] = userSet.Values.ToArray();                
                j++;
            }
            
            BackPropagationLearning teacher = new BackPropagationLearning(network);
            // loop
            double error = 100;
            int count = 1;
            while (error > .01 && count < 100000)
            {
                // run epoch of learning procedure
                error = teacher.RunEpoch(tempIn, output);                
                // check error value to see if we need to stop
                count++;
            }

            return error;
        }

        static double[] FireNetwork(List<string> words, Dictionary<string, double> userWordsDict)
        {
            var tempIn = new List<double>();

            foreach (var word in words)
            {
                if (userWordsDict.ContainsKey(word))
                    tempIn.Add(userWordsDict[word]);
                else
                    tempIn.Add(0);
            }

            var input = tempIn.ToArray();
            var max = Math.Max(input.Max(), .1);
            input.Select(i => i / max);
            
            return network.Compute(input);
        }
    }
}
