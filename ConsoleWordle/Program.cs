using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace ConsoleWordle
{
    internal class Program
    {
        enum LetterStatus
        {
            Correct,
            WrongPlace,
            Incorrect,
        }

        static void Main(string[] args)
        {
            // The game needs these variables. You can create some of your own if you need them, but don't remove these ones.
            Random _rng = new Random();   //RNG generator

            List<String> wordList = LoadWords();  //List of words loaded from file.

            String mysteryWord = wordList[_rng.Next(wordList.Count)];  //select the initial mystery word from the list - at random.
            string currentGuessWord = "";  //AI guess word
            int wordGuessCount = 0;  //count how many guesses
            int totalGuessCount = 0; //keep track of total guesses for average guess count
            int wordCount = 1;  //keep track of how many words have been guessed correctly
            float avgGuessCount = 0;  //keep track of average guess count

            List<String> AIWordList = LoadWords();  //**AI NOTE: i created this for you to use. It's a copy of the word list that the AI can access and/or change.
            LetterStatus[] AIStatusTracker;  //This is an array of 5 elements that tracks the status of each letter in a guessed word. You can use this to help your AI figure out what to guess next.

            //This stores the AI's previous guesses and the feedback it received.
            //The AI uses this information to narrow down the possible answers.
            List<(string guess, LetterStatus[] status)> guessHistory
                = new List<(string guess, LetterStatus[] status)>();

            while (wordCount <= 1000)  //changed to 1000 for the final. 
            {
                //Leave this alone. It counts how many words have been guessed.
                wordGuessCount++;

                //currentGuessWord = GetWord("What is your guess: ");  //commented this out when you are working with your AI


                //AI START -- this is where your first guess goes. Not subsequent guesses. So find a way to skip this 
                //when it's not the first guess.

                //The AI starts with "crane" because it has a mix of common letters.               
                if (guessHistory.Count == 0)
                {
                    if (AIWordList.Contains("crane"))
                    {
                        currentGuessWord = "crane";
                    }
                    else
                    {
                        currentGuessWord = AIWordList[_rng.Next(AIWordList.Count)];
                    }
                }
                else
                {
                    //After the first guess, the AI chooses a word from the remaining candidates.
                    currentGuessWord = ChooseBestGuess(AIWordList);
                }

                //AI FINISH


                //Leave this alone. It checks to see if the word you guessed is a valid word. This will work for your AI too. If the guessed word
                //isn't a real word, the loop will restart without costing a guess.
                if (wordList.Contains(currentGuessWord) == false)
                {
                    Console.WriteLine("Not a valid word, try again.");
                    wordGuessCount--;
                    continue;
                }


                //**AI Note: You can use AIStatusTracker to "see" what's right and what's wrong in your guessed word before checking your next word.
                //ALSO CheckWord is what checks the guessed word against the mystery word. Try not to remove or change this unless you REALLY know what you're doing.
                AIStatusTracker = CheckWord(mysteryWord, currentGuessWord);


                //AI START -- this is where your next guess goes.

                //Save the guess and its feedback so the AI can use it on the next turn.
                //Clone the array so the feedback from this guess stays the same.
                guessHistory.Add(
                    (currentGuessWord, (LetterStatus[])AIStatusTracker.Clone()));

                //Remove words that do not match the feedback from the guesses so far.
                //The remaining list contains the words that could still be the answer.
                AIWordList = AIWordList
                    .Where(candidate => guessHistory.All(previous =>
                        MatchesFeedback(
                            candidate,
                            previous.guess,
                            previous.status)))
                    .ToList();

                //AI FINISH


                //Leave this alone. It tells you when you found the word, updates the counters, and moves on to the next word.
                if (mysteryWord.Equals(currentGuessWord))
                {
                    totalGuessCount += wordGuessCount;
                    avgGuessCount = (float)totalGuessCount / wordCount;
                    Console.WriteLine("Congrats, you got it in " + wordGuessCount + "! Avg guess count = " + avgGuessCount + " Resetting the mystery wordle.\n");
                    mysteryWord = wordList[_rng.Next(wordList.Count)];
                    AIWordList = LoadWords();

                    //Clear the previous guesses because the next puzzle has a different answer.
                    guessHistory.Clear();

                    wordCount++;
                    wordGuessCount = 0;
                }
            }

            //Show the final results after all the test puzzles are complete.
            Console.WriteLine("ALL WORDLES COMPLETE!!.");
            Console.WriteLine("Total number of guesses: " + totalGuessCount);
            Console.WriteLine("Average guesses per wordle: " + avgGuessCount);
        }


        //This method chooses the next word for the AI to guess.
        //It scores words based on how often their letters appear in the remaining candidates.
        static string ChooseBestGuess(List<string> candidates)
        {
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    "The AI has no possible words left. Check the feedback logic.");
            }

            //Count how often each letter appears across the remaining words.
            Dictionary<char, int> letterFrequency =
                new Dictionary<char, int>();

            foreach (string word in candidates)
            {
                //Only count each letter once per word.
                foreach (char letter in word.Distinct())
                {
                    if (!letterFrequency.ContainsKey(letter))
                    {
                        letterFrequency[letter] = 0;
                    }

                    letterFrequency[letter]++;
                }
            }

            //Give each word a score based on the letters it contains.
            //Words with more common letters get a higher score.
            string bestWord = candidates[0];
            int bestScore = -1;

            foreach (string word in candidates)
            {
                int score = 0;

                foreach (char letter in word.Distinct())
                {
                    score += letterFrequency[letter];
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestWord = word;
                }
            }

            return bestWord;
        }


        //This checks if a possible answer matches the feedback from an earlier guess.
        //If the feedback does not match, the word cannot be the answer.
        static bool MatchesFeedback(
            string candidate,
            string previousGuess,
            LetterStatus[] previousStatus)
        {
            LetterStatus[] predictedStatus =
                GetFeedback(candidate, previousGuess);

            for (int i = 0; i < predictedStatus.Length; i++)
            {
                if (predictedStatus[i] != previousStatus[i])
                {
                    return false;
                }
            }

            return true;
        }


        //This works out what feedback a guess would receive for a possible answer.
        //It follows the same letter-checking rules as the original CheckWord method.
        static LetterStatus[] GetFeedback(string word, string guess)
        {
            char[] wordChars = word.ToCharArray();
            char[] guessChars = guess.ToCharArray();

            LetterStatus[] statusTracker = new LetterStatus[5];

            //Start by marking every letter as incorrect.
            statusTracker[0] = LetterStatus.Incorrect;
            statusTracker[1] = LetterStatus.Incorrect;
            statusTracker[2] = LetterStatus.Incorrect;
            statusTracker[3] = LetterStatus.Incorrect;
            statusTracker[4] = LetterStatus.Incorrect;

            //Keep track of letters that have not already been matched.
            List<char> letterCounter = word.ToList();

            //Green letters: the letter is in the correct position.
            for (int i = 0; i < guessChars.Length; i++)
            {
                for (int j = 0; j < wordChars.Length; j++)
                {
                    if (guessChars[i] == wordChars[j] && i == j)
                    {
                        statusTracker[i] = LetterStatus.Correct;
                        letterCounter.Remove(guessChars[i]);
                        break;
                    }
                }
            }

            //Yellow letters: the letter is in the word but in the wrong position.
            for (int i = 0; i < guessChars.Length; i++)
            {
                for (int j = 0; j < wordChars.Length; j++)
                {
                    if (statusTracker[i] == LetterStatus.Correct)
                    {
                        break;
                    }

                    if (guessChars[i] == wordChars[j]
                        && i != j
                        && statusTracker[j] != LetterStatus.Correct)
                    {
                        if (letterCounter.Contains(guessChars[i]))
                        {
                            statusTracker[i] = LetterStatus.WrongPlace;
                            letterCounter.Remove(guessChars[i]);
                        }
                    }
                }
            }

            return statusTracker;
        }


        //Check the guessed word against the mystery word.
        //Don't change this function without asking me first.
        static LetterStatus[] CheckWord(String word, String guess)
        {
            char[] wordChars = word.ToCharArray();      //convert mystery word and guessed word to char arrays for easier comparison.
            char[] guessChars = guess.ToCharArray();

            LetterStatus[] statusTracker = new LetterStatus[5]; //an array to hold the status of each guessed letter.
            //assume all the guessed letters start "incorrect"
            statusTracker[0] = LetterStatus.Incorrect;
            statusTracker[1] = LetterStatus.Incorrect;
            statusTracker[2] = LetterStatus.Incorrect;
            statusTracker[3] = LetterStatus.Incorrect;
            statusTracker[4] = LetterStatus.Incorrect;

            //This is part of the world rule check. This is used to deal with multiple green or yellow letters. Don't remove it.
            List<char> letterCounter = word.ToList();

            //green letters - right letter, right place
            for (int i = 0; i < guessChars.Length; i++)
            {
                for (int j = 0; j < wordChars.Length; j++)
                {
                    if (guessChars[i] == wordChars[j] && i == j)  //right letter, right place
                    {
                        statusTracker[i] = LetterStatus.Correct;  //set the status in the array
                        letterCounter.Remove(guessChars[i]);      //remove the letter from the counter so it can't be flagged yellow later
                        break;                                    //stop the loop since we found a match for this letter
                    }
                }
            }

            //yellow letters - right letter wrong place
            for (int i = 0; i < guessChars.Length; i++)
            {
                for (int j = 0; j < wordChars.Length; j++)
                {
                    if (statusTracker[i] == LetterStatus.Correct)       //if the letter is already flagged green, don't check it again
                        break;

                    if (guessChars[i] == wordChars[j] && i != j && statusTracker[j] != LetterStatus.Correct)    //right letter, wrong place and not already flagged green (correct)
                    {
                        //count occurences of that letter, only flag yellow if flags < actual occurences
                        if (letterCounter.Contains(guessChars[i]))
                        {
                            statusTracker[i] = LetterStatus.WrongPlace;  //set the status in the array
                            letterCounter.Remove(guessChars[i]);        //remove the letter from future consideration so it can't be flagged yellow again
                        }
                    }
                }
            }

            //print the guessed word with colors for correct and wrong place letters.
            for (int i = 0; i < guessChars.Length; i++)
            {
                //set the color
                if (statusTracker[i] == LetterStatus.Correct)
                    Console.ForegroundColor = ConsoleColor.Green;
                else if (statusTracker[i] == LetterStatus.WrongPlace)
                    Console.ForegroundColor = ConsoleColor.Yellow;
                else
                    Console.ForegroundColor = ConsoleColor.White;

                Console.Write(guessChars[i]);  //print the letter
                Console.ForegroundColor = ConsoleColor.White;  //reset back to white for the next letter
            }

            Console.WriteLine();        

            return statusTracker;       //return the arrray - mostly of use for the AI
        }


        //This method loads the words from file. Don't change
        //this without asking me first.
        static List<string> LoadWords()
        {
            List<string> wordList = new List<string>();
            string[] lines = File.ReadAllLines("../../../words.txt");

            foreach (string line in lines)
            {
                if (line.Length == 5)
                {
                    wordList.Add(line.ToLower());
                }
            }

            return wordList;
        }


        //This is the original human player GetWord method.
        //You can write an AI version, if it's helpful for
        //you.
        static string GetWord(String prompt)
        {
            Console.Write(prompt);
            string inputWord = Console.ReadLine();

            while (inputWord.Length != 5)
            {
                Console.Write("Word must be 5 letters. Try again:");
                inputWord = Console.ReadLine();
            }

            return inputWord.ToLower();
        }
    }
}
