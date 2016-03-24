﻿using Newtonsoft.Json;
using System;
using System.Reflection;

namespace Microsoft.Research.MultiWorldTesting.ExploreLibrary
{
    public sealed class EpsilonGreedyState : GenericExplorerState
    {
        [JsonProperty(PropertyName = "e")]
        public float Epsilon { get; set; }
        
        [JsonProperty(PropertyName = "isExplore")]
        public bool IsExplore { get; set; }
    }

    public static class EpsilonGreedy
    {
        // TODO: move me down
        public static void Explore(ref uint outAction, ref float outProbability, ref bool outShouldLog, ref bool outIsExplore,
            uint numActions, bool explore, float defaultEpsilon, ulong saltedSeed)
        {
            var random = new PRG(saltedSeed);
            float epsilon = explore ? defaultEpsilon : 0f;

            float baseProbability = epsilon / numActions; // uniform probability

            if (random.UniformUnitInterval() < 1f - epsilon)
            {
                outProbability = 1f - epsilon + baseProbability;
            }
            else
            {
                // Get uniform random 1-based action ID
                uint actionId = (uint)random.UniformInt(1, numActions);

                if (actionId == outAction)
                {
                    // If it matches the one chosen by the default policy
                    // then increase the probability
                    outProbability = 1f - epsilon + baseProbability;
                }
                else
                {
                    // Otherwise it's just the uniform probability
                    outProbability = baseProbability;
                }
                outAction = actionId;
                outIsExplore = true;
            }

            outShouldLog = true;
        }
    }

    /// <summary>
    /// The epsilon greedy exploration class.
    /// </summary>
    /// <remarks>
    /// This is a good choice if you have no idea which actions should be preferred.
    /// Epsilon greedy is also computationally cheap.
    /// </remarks>
    /// <typeparam name="TContext">The Context type.</typeparam>
    public class EpsilonGreedyExplorer<TContext, TPolicyState> : BaseExplorer<TContext, uint, EpsilonGreedyState, uint, TPolicyState>
    {
        private readonly float epsilon;

        /// <summary>
        /// The constructor is the only public member, because this should be used with the MwtExplorer.
        /// </summary>
        /// <param name="defaultPolicy">A default function which outputs an action given a context.</param>
        /// <param name="epsilon">The probability of a random exploration.</param>
        /// <param name="numActions">The number of actions to randomize over.</param>
        public EpsilonGreedyExplorer(IPolicy<TContext, int, TPolicyState> defaultPolicy, float epsilon, uint numActions = uint.MaxValue)
            : base(defaultPolicy, numActions)
        {
            if (epsilon < 0 || epsilon > 1)
            {
                throw new ArgumentException("Epsilon must be between 0 and 1.");
            }
            this.epsilon = epsilon;
        }

        protected override Decision<int, EpsilonGreedyState, TPolicyState> ChooseActionInternal(ulong saltedSeed, TContext context, uint numActionsVariable)
        {
            // Invoke the default policy function to get the action
            PolicyDecision<uint, TPolicyState> policyDecisionTuple = this.defaultPolicy.ChooseAction(context, numActions);
            uint chosenAction = policyDecisionTuple.Action;

            if (chosenAction == 0 || chosenAction > numActions)
            {
                throw new ArgumentException("Action chosen by default policy is not within valid range.");
            }

            float actionProbability = 0f;
            bool shouldRecord = false;
            bool isExplore = false;

            EpsilonGreedy.Explore(ref chosenAction, ref actionProbability, ref shouldRecord, ref isExplore,
                numActions, this.explore, this.epsilon, saltedSeed);

            EpsilonGreedyState explorerState = new EpsilonGreedyState 
            { 
                Epsilon = this.epsilon, 
                IsExplore = isExplore,
                Probability = actionProbability
            };

            return Decision.Create(chosenAction, explorerState, policyDecisionTuple.PolicyState);
        }
    }
}

/*
namespace Microsoft.Research.MultiWorldTesting.ExploreLibrary.SingleAction
{
    using Microsoft.Research.MultiWorldTesting.ExploreLibrary.Core;

    /// <summary>
	/// The epsilon greedy exploration class.
	/// </summary>
	/// <remarks>
	/// This is a good choice if you have no idea which actions should be preferred.
	/// Epsilon greedy is also computationally cheap.
	/// </remarks>
	/// <typeparam name="TContext">The Context type.</typeparam>
	public class EpsilonGreedyExplorer<TContext> : IExplorer<TContext>, IConsumePolicy<TContext>
	{
        private IPolicy<TContext> defaultPolicy;
        private readonly float epsilon;
        private bool explore;
        private readonly uint numActionsFixed;

		/// <summary>
		/// The constructor is the only public member, because this should be used with the MwtExplorer.
		/// </summary>
		/// <param name="defaultPolicy">A default function which outputs an action given a context.</param>
		/// <param name="epsilon">The probability of a random exploration.</param>
		/// <param name="numActions">The number of actions to randomize over.</param>
        public EpsilonGreedyExplorer(IPolicy<TContext> defaultPolicy, float epsilon, uint numActions)
		{
            VariableActionHelper.ValidateInitialNumberOfActions(numActions);

            if (epsilon < 0 || epsilon > 1)
            {
                throw new ArgumentException("Epsilon must be between 0 and 1.");
            }
            this.defaultPolicy = defaultPolicy;
            this.epsilon = epsilon;
            this.numActionsFixed = numActions;
            this.explore = true;
        }

        /// <summary>
        /// Initializes an epsilon greedy explorer with variable number of actions.
        /// </summary>
        /// <param name="defaultPolicy">A default function which outputs an action given a context.</param>
        /// <param name="epsilon">The probability of a random exploration.</param>
        public EpsilonGreedyExplorer(IPolicy<TContext> defaultPolicy, float epsilon) :
            this(defaultPolicy, epsilon, uint.MaxValue)
        { }

        public void UpdatePolicy(IPolicy<TContext> newPolicy)
        {
            this.defaultPolicy = newPolicy;
        }

        public void EnableExplore(bool explore)
        {
            this.explore = explore;
        }

        public DecisionTuple ChooseAction(ulong saltedSeed, TContext context, uint numActionsVariable = uint.MaxValue)
        {
            uint numActions = VariableActionHelper.GetNumberOfActions(this.numActionsFixed, numActionsVariable);

            // Invoke the default policy function to get the action
            PolicyDecisionTuple policyDecisionTuple = this.defaultPolicy.ChooseAction(context, numActions);
            uint chosenAction = policyDecisionTuple.Action;

            if (chosenAction == 0 || chosenAction > numActions)
            {
                throw new ArgumentException("Action chosen by default policy is not within valid range.");
            }

            float actionProbability = 0f;
            bool shouldRecord = false;
            bool isExplore = false;

            EpsilonGreedy.Explore(ref chosenAction, ref actionProbability, ref shouldRecord, ref isExplore,
                numActions, this.explore, this.epsilon, saltedSeed);

            return new DecisionTuple 
            { 
                Action = chosenAction, 
                Probability = actionProbability,
                ShouldRecord = shouldRecord,
                ModelId = policyDecisionTuple.ModelId,
                IsExplore = isExplore
            };
        }
    };

    /// <summary>
    /// The epsilon greedy exploration class.
    /// </summary>
    /// <remarks>
    /// This is a good choice if you have no idea which actions should be preferred.
    /// Epsilon greedy is also computationally cheap.
    /// </remarks>
    /// <typeparam name="TContext">The Context type.</typeparam>
    public class EpsilonGreedyExplorer<TContext> : IExplorer<TContext, int>, IConsumePolicy<TContext>
    {
    }
}

namespace Microsoft.Research.MultiWorldTesting.ExploreLibrary.MultiAction
{
    using Microsoft.Research.MultiWorldTesting.ExploreLibrary.Core;

    /// <summary>
    /// The epsilon greedy exploration class.
    /// </summary>
    /// <remarks>
    /// This is a good choice if you have no idea which actions should be preferred.
    /// Epsilon greedy is also computationally cheap.
    /// </remarks>
    /// <typeparam name="TContext">The Context type.</typeparam>
    public class EpsilonGreedyExplorer<TContext> : IExplorer<TContext>, IConsumePolicy<TContext>
    {
        private IPolicy<TContext> defaultPolicy;
        private readonly float epsilon;
        private bool explore;
        private readonly uint numActionsFixed;

        /// <summary>
        /// The constructor is the only public member, because this should be used with the MwtExplorer.
        /// </summary>
        /// <param name="defaultPolicy">A default function which outputs an action given a context.</param>
        /// <param name="epsilon">The probability of a random exploration.</param>
        /// <param name="numActions">The number of actions to randomize over.</param>
        public EpsilonGreedyExplorer(IPolicy<TContext> defaultPolicy, float epsilon, uint numActions)
        {
            VariableActionHelper.ValidateInitialNumberOfActions(numActions);

            if (epsilon < 0 || epsilon > 1)
            {
                throw new ArgumentException("Epsilon must be between 0 and 1.");
            }
            this.defaultPolicy = defaultPolicy;
            this.epsilon = epsilon;
            this.numActionsFixed = numActions;
            this.explore = true;
        }

        /// <summary>
        /// Initializes an epsilon greedy explorer with variable number of actions.
        /// </summary>
        /// <param name="defaultPolicy">A default function which outputs an action given a context.</param>
        /// <param name="epsilon">The probability of a random exploration.</param>
        public EpsilonGreedyExplorer(IPolicy<TContext> defaultPolicy, float epsilon) :
            this(defaultPolicy, epsilon, uint.MaxValue)
        { }

        public void UpdatePolicy(IPolicy<TContext> newPolicy)
        {
            this.defaultPolicy = newPolicy;
        }

        public void EnableExplore(bool explore)
        {
            this.explore = explore;
        }

        public DecisionTuple ChooseAction(ulong saltedSeed, TContext context, uint numActionsVariable = uint.MaxValue)
        {
            uint numActions = VariableActionHelper.GetNumberOfActions(this.numActionsFixed, numActionsVariable);

            // Invoke the default policy function to get the action
            PolicyDecisionTuple policyDecisionTuple = this.defaultPolicy.ChooseAction(context, numActions);
            uint[] chosenActions = policyDecisionTuple.Actions;

            MultiActionHelper.ValidateActionList(chosenActions);

            uint topAction = chosenActions[0];
            float actionProbability = 0f;
            bool shouldRecord = false;
            bool isExplore = false;

            EpsilonGreedy.Explore(ref topAction, ref actionProbability, ref shouldRecord, ref isExplore,
                numActions, this.explore, this.epsilon, saltedSeed);

            // Put chosen action at the top of the list, swapping out the current top.
            MultiActionHelper.PutActionToList(topAction, chosenActions);

            return new DecisionTuple
            {
                Actions = chosenActions,
                Probability = actionProbability,
                ShouldRecord = shouldRecord,
                ModelId = policyDecisionTuple.ModelId,
                IsExplore = isExplore
            };
        }
    };
}