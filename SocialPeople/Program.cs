﻿using AForge.Neuro;
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
        static void Main(string[] args)
        {
            var split = RemoveStopWords(@"Got home a few hours ago and already had to take Kermit to the vet. He's okay don't worry, it'll be in the vlog tonight. But poor guy 😭

Happy Thanksgiving! Spending the day cooking and laughing and hugging dogs and being infinitely grateful. 🐩🐩🐩🐩🐩🐩

Video's up: Oops I'm In Tokyo https://www.youtube.com/watch?v=JW70oTiQbm8&feature=youtu.be …

I'm home now though, yayyyy happy thanksgiving!");

            var x = GetNGrams(split,2);
            var y = GetDictionary(x);
            SetupNeuralNetwork(y);
        }

        static string[] STOP_WORDS = new string[26] { "a", "an", "and", "are", "as", "at", "be", "by", "for", "from", "has", "he", "in", "is", "it", "its", "of", "on", "she", "that", "the", "to", "was", "were", "will", "with"};
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
            for(int i = 0; i < input.Length - 1; i++)
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

        static Dictionary<string,int> GetDictionary(string [] input)
        {
            var dict = new Dictionary<string, int>();

            foreach (var value in input)
            {
                if (dict.ContainsKey(value))
                    dict[value]++;
                else
                    dict[value] = 1;
            }

            return dict;
        }

        static void SetupNeuralNetwork(Dictionary<string,double> wordsDict, Dictionary<string,double> user)
        {
            // initialize input and output values
            //double[][] input = new double[4][] {
            //    new double[] {0, 0}, new double[] {0, 1},
            //    new double[] {1, 0}, new double[] {1, 1}
            //};

            double[][] output = new double[4][] {
                new double[] {0}, new double[] {1},
                new double[] {1}, new double[] {0}
            };

            double[] input = new double[wordsDict.Count];
            wordsDict.Values.CopyTo(input, 0);
            var max = Math.Max(input.Max(), .1);
            input.Select(i => i / max);

            // create neural network
            ActivationNetwork network = new ActivationNetwork(
                new SigmoidFunction(2),
                2, // two inputs in the network
                2, // two neurons in the first layer
                1); // one neuron in the second layer
                    // create teacher
            BackPropagationLearning teacher = new BackPropagationLearning(network);
            // loop
            double error = 100;
            while (error > .01)
            {
                // run epoch of learning procedure
                error = teacher.RunEpoch(input, output);
                // check error value to see if we need to stop
                // ...
            }

            var t = network.Compute(new double[] { 0, 1 });
            var f = network.Compute(new double[] { 1, 1 });
        }
    }
}
