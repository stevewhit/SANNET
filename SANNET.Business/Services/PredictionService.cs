﻿using Framework.Generic.EntityFramework;
using Framework.Generic.Utility;
using NeuralNetwork.Generic.Datasets;
using NeuralNetwork.Generic.Layers;
using NeuralNetwork.Generic.Networks;
using NeuralNetwork.Generic.Neurons;
using SANNET.DataModel;
using StockMarket.DataModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace SANNET.Business.Services
{
    public interface IPredictionService<P> : IDisposable where P : Prediction
    {
        /// <summary>
        /// Returns stored predictions.
        /// </summary>
        /// <returns>Returns predictions stored in the repository.</returns>
        IDbSet<P> GetPredictions();

        /// <summary>
        /// Finds and returns the prediction with the matching id.
        /// </summary>
        /// <param name="id">The id of the prediction to return.</param>
        /// <returns>Returns the prediction with the matching id.</returns>
        P FindPrediction(int id);

        /// <summary>
        /// Adds the supplied <paramref name="prediction"/>.
        /// </summary>
        /// <param name="prediction">The prediction that is to be added.</param>
        void Add(P prediction);

        /// <summary>
        /// Adds the supplied <paramref name="predictions"/> to the repository.
        /// </summary>
        /// <param name="predictions">The predictions that are to be added.</param>
        void AddRange(IEnumerable<P> predictions);

        /// <summary>
        /// Updates the supplied <paramref name="prediction"/>.
        /// </summary>
        /// <param name="prediction">The prediction that is to be updated.</param>
        void Update(P prediction);

        /// <summary>
        /// Finds and deletes an existing prediction by <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id of prediction to be deleted.</param>
        void Delete(int id);

        /// <summary>
        /// Deletes the supplied <paramref name="prediction"/>.
        /// </summary>
        /// <param name="prediction">The prediction that is to be deleted.</param>
        void Delete(P prediction);
    }

    public class PredictionService<P, Q, C, N> : IPredictionService<P> where P : Prediction, new() where Q : Quote where C : Company where N : NetworkConfiguration
    {
        private bool _isDisposed = false;
        private const string PREDICTION_DESCRIPTION_FORMAT = "[({0}%) {1}]";

        private readonly IEfRepository<P> _predictionRepository;
        private readonly IQuoteService<Q> _quoteService;
        private readonly ICompanyService<C> _companyService;
        private readonly IDatasetService _datasetService;
        private readonly INetworkConfigurationService<N> _networkConfigurationService;

        public PredictionService(IEfRepository<P> predictionRepository, IQuoteService<Q> quoteService, ICompanyService<C> companyService, IDatasetService datasetService, INetworkConfigurationService<N> networkConfigurationService)
        {
            _predictionRepository = predictionRepository ?? throw new ArgumentNullException("predictionRepository");
            _quoteService = quoteService ?? throw new ArgumentNullException("quoteService");
            _companyService = companyService ?? throw new ArgumentNullException("companyService");
            _datasetService = datasetService ?? throw new ArgumentNullException("datasetService");
            _networkConfigurationService = networkConfigurationService ?? throw new ArgumentNullException("networkConfigurationService");
        }

        #region IPredictionService<P>

        /// <summary>
        /// Returns stored predictions.
        /// </summary>
        /// <returns>Returns predictions stored in the repository.</returns>
        public IDbSet<P> GetPredictions()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("PredictionService", "The service has been disposed.");

            return _predictionRepository.GetEntities();
        }

        /// <summary>
        /// Finds and returns the prediction with the matching id.
        /// </summary>
        /// <param name="id">The id of the prediction to return.</param>
        /// <returns>Returns the prediction with the matching id.</returns>
        public P FindPrediction(int id)
        {
            return GetPredictions().FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// Adds the supplied <paramref name="prediction"/>.
        /// </summary>
        /// <param name="prediction">The prediction that is to be added.</param>
        public void Add(P prediction)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("PredictionService", "The service has been disposed.");

            if (prediction == null)
                throw new ArgumentNullException("prediction");

            _predictionRepository.Add(prediction);
            _predictionRepository.SaveChanges();
        }

        /// <summary>
        /// Adds the supplied <paramref name="predictions"/> to the repository.
        /// </summary>
        /// <param name="predictions">The predictions that are to be added.</param>
        public void AddRange(IEnumerable<P> predictions)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("PredictionService", "The service has been disposed.");

            if (predictions == null)
                throw new ArgumentNullException("predictions");

            _predictionRepository.AddRange(predictions);
            _predictionRepository.SaveChanges();
        }

        /// <summary>
        /// Updates the supplied <paramref name="prediction"/>.
        /// </summary>
        /// <param name="prediction">The prediction that is to be updated.</param>
        public void Update(P prediction)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("PredictionService", "The service has been disposed.");

            if (prediction == null)
                throw new ArgumentNullException("prediction");

            _predictionRepository.Update(prediction);
            _predictionRepository.SaveChanges();
        }

        /// <summary>
        /// Finds and deletes an existing prediction by <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The id of prediction to be deleted.</param>
        public void Delete(int id)
        {
            var prediction = FindPrediction(id);

            if (prediction == null)
                throw new ArgumentException($"A prediction with the supplied id doesn't exist: {id}.");

            Delete(prediction);
        }

        /// <summary>
        /// Deletes the supplied <paramref name="prediction"/>.
        /// </summary>
        /// <param name="prediction">The prediction that is to be deleted.</param>
        public void Delete(P prediction)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("PredictionService", "The service has been disposed.");

            if (prediction == null)
                throw new ArgumentNullException("prediction");

            _predictionRepository.Delete(prediction);
            _predictionRepository.SaveChanges();
        }

        /// <summary>
        /// Generates quote predictions for all quotes, in EACH network configuration.
        /// </summary>
        public void GenerateAllPredictions()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("PredictionService", "The service has been disposed.");

            var quotes = _quoteService.GetQuotes().ToList();
            var networkConfigs = _networkConfigurationService.GetConfigurations().ToList();
            var existingPredictions = GetPredictions().ToList();
            var newPredictions = new List<P>();
            
            // Generate predictions for all quotes, in EACH network configuration.
            foreach (var quote in quotes.Where(q => q.Id == 2500))
            {
                foreach (var config in networkConfigs.Where(c => c.Id == 3))
                {
                    // If the prediction for this quote and network config doesn't exist, create one.
                    if (!existingPredictions.Any(p => p.QuoteId == quote.Id && p.NetworkConfigurationId == config.Id))
                    {
                        newPredictions.Add(GenerateQuotePrediction(quote, config));
                    }
                }
            }

            AddRange(newPredictions.Where(p => p != null));
        }

        /// <summary>
        /// Generates and returns a prediction for a specific <paramref name="quote"/> using the specified <paramref name="networkConfig"/>. Otherwise, NULL if a valid training dataset could not be created.
        /// </summary>
        /// <param name="quote">The quote to generate predictions for.</param>
        /// <param name="networkConfig">The network configuration used to generate the prediction.</param>
        /// <returns>Returns the prediction for the specified <paramref name="quote"/> using the specified <paramref name="networkConfig"/>. Otherwise, NULL if a valid training dataset could not be created.</returns>
        public P GenerateQuotePrediction(Q quote, N networkConfig)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("PredictionService", "The service has been disposed.");

            if (quote == null)
                throw new ArgumentNullException("quote");

            if (networkConfig == null)
                throw new ArgumentNullException("networkConfig");

            // Create and train a network
            var trainedNetwork = CreateTrainedNetwork(networkConfig, quote);
            if (trainedNetwork == null)
                return null;

            // Apply test inputs to network to retrieve outputs.
            var outputs = ApplyConfigInputsToNetwork(trainedNetwork, networkConfig, quote.CompanyId, quote.Date);

            return new P()
            {
                NetworkConfigurationId = networkConfig.Id,
                CompanyId = quote.CompanyId,
                QuoteId = quote.Id,
                TrainingStartDate = GetTrainingStartDateForQuote(quote, networkConfig),
                TrainingEndDate = GetTrainingEndDateForQuote(quote),
                PredictedOutcome = string.Join(",", outputs.Select(o => string.Format(PREDICTION_DESCRIPTION_FORMAT, o.ActivationLevel, o.Description)))
            };
        }
        
        /// <summary>
        /// Creates and returns a trained neural network using the specified <paramref name="networkConfiguration"/> over the identified training dates. 
        /// Note: Will return NULL if a valid training dataset could not be created.
        /// </summary>
        /// <param name="networkConfiguration">The network configuration for the neural network.</param>
        /// <param name="companyId">The id of the company </param>
        /// <param name="trainingStartDate"></param>
        /// <param name="trainingEndDate"></param>
        /// <returns>Returns a trained neural network using the specified <paramref name="networkConfiguration"/> over the identified training dates. Otherwise, NULL if a valid training dataset could not be created.</returns>
        private IDFFNeuralNetwork CreateTrainedNetwork(N networkConfiguration, Q quote)
        {
            var trainingStartDate = GetTrainingStartDateForQuote(quote, networkConfiguration);
            var trainingEndDate = GetTrainingEndDateForQuote(quote);
            var trainingDatasetEntries = _datasetService.GetTrainingDataset(networkConfiguration.DatasetRetrievalMethodId, quote.CompanyId, trainingStartDate, trainingEndDate);

            // Return null to indicate that a valid training dataset couldn't be created.
            // This should only occur when the start and/or end dates are invalid.
            if (trainingDatasetEntries == null || trainingDatasetEntries.Count() == 0)
                return null;

            // Create a neural network using the network config and dataset entries.
            var network = CreateNetwork(networkConfiguration, trainingDatasetEntries.First());

            // Sync dataset NeuronIds with the network NeuronIds.
            MapTrainingEntriesToNeurons(network, trainingDatasetEntries);

            return TrainNetwork(network, trainingDatasetEntries.ToList());
        }

        /// <summary>
        /// Creates and returns a neural network using configuration settings in the <paramref name="networkConfiguration"/> and the <paramref name="trainingEntry"/>. 
        /// </summary>
        /// <param name="networkConfiguration"></param>
        /// <param name="trainingDatasetEntries"></param>
        /// <returns>Returns a neural network using configuration settings in the <paramref name="networkConfiguration"/> and the <paramref name="trainingEntry"/>.</returns>
        private IDFFNeuralNetwork CreateNetwork(N networkConfiguration, INetworkTrainingIteration trainingEntry)
        {
            if (trainingEntry == null || trainingEntry.Inputs == null || trainingEntry.Outputs == null)
                throw new ArgumentNullException("trainingEntry");

            var numInputs = trainingEntry.Inputs.Count();
            var numOutputs = trainingEntry.Outputs.Count();
            var numHiddenLayers = networkConfiguration.NumHiddenLayers;
            var numHiddenLayerNeurons = networkConfiguration.NumHiddenLayerNeurons;

            // Setup and randomize network.
            var network = new DFFNeuralNetwork(numInputs, numHiddenLayers, numHiddenLayerNeurons, numOutputs);
            network.RandomizeNetwork();

            return network;
        }

        /// <summary>
        /// Trains the <paramref name="network"/> using <paramref name="trainingDatasetEntries"/> and return the average training cost of the final iteration.
        /// </summary>
        /// <param name="network">The neural network that is to be trained.</param>
        /// <param name="trainingDatasetEntries">The training iterations to train the network with.</param>
        /// <returns>Returns the average training cost of the final iteration.</returns>
        private IDFFNeuralNetwork TrainNetwork(IDFFNeuralNetwork network, IList<INetworkTrainingIteration> trainingDatasetEntries)
        {
            //var dataset = trainingDatasetEntries.ToList();
            //dataset.AddRange(trainingDatasetEntries);
            //dataset.AddRange(trainingDatasetEntries);
            //dataset.AddRange(trainingDatasetEntries);
            //dataset.AddRange(trainingDatasetEntries);
            //dataset.AddRange(trainingDatasetEntries);
            //dataset.AddRange(trainingDatasetEntries);

            var iterations = 0;
            var bestTrainingCost = double.MaxValue;
            while (true)
            {
                trainingDatasetEntries.Shuffle();

                // TODO: Implement Clone() method for NeuralNetwork and store the BEST network.
                var trainingCost = network.Train(trainingDatasetEntries).Average(i => i.TrainingCost);
                if (trainingCost >= bestTrainingCost)
                    break;

                bestTrainingCost = trainingCost;
                iterations++;

                Console.WriteLine(trainingCost);

                var inputs = trainingDatasetEntries.ToList()[11].Inputs;
                var outputs2 = network.ApplyInputs(inputs).ToList();
            }

            //var inputs = trainingDatasetEntries.ToList()[5].Inputs;
            //var dataset = trainingDatasetEntries.ToList();
            //dataset.AddRange(trainingDatasetEntries);
            //dataset.AddRange(trainingDatasetEntries);
            //dataset.AddRange(trainingDatasetEntries);
            //dataset.AddRange(trainingDatasetEntries);
            //dataset.AddRange(trainingDatasetEntries);
            //dataset.AddRange(trainingDatasetEntries);

            //for (int i = 0; i < 10000; i++)
            //{
            //    trainingDatasetEntries.Shuffle();
            //    var cost = network.Train(trainingDatasetEntries).Average(e => e.TrainingCost);

            //    //var outputs = network.ApplyInputs(inputs).ToList();
            //}

            //// Expect 0 1 1 0
            //var outputs2 = network.ApplyInputs(inputs).ToList();

            return network;
        }

        /// <summary>
        /// Maps each of the training entry inputs and outputs to their respective input and output neurons in the network.
        /// </summary>
        /// <param name="network">The network containing the input and output neurons to map the entries to.</param>
        /// <param name="trainingDatasetEntries">The training entries with inputs and outputs that will be mapped to network neurons.</param>
        private void MapTrainingEntriesToNeurons(INeuralNetwork network, IEnumerable<INetworkTrainingIteration> trainingDatasetEntries)
        {
            if (network == null)
                throw new ArgumentNullException("network");

            if (trainingDatasetEntries == null)
                throw new ArgumentNullException("trainingDatasetEntries");

            var inputNeurons = network.Layers?.OfType<IInputLayer>().FirstOrDefault()?.Neurons?.OfType<IInputNeuron>().ToList();
            var outputNeurons = network.Layers?.OfType<IOutputLayer>().FirstOrDefault()?.Neurons?.OfType<IOutputNeuron>().ToList();

            // Link the network inputs/outputs to the input/output neurons of the network.
            foreach (var trainingEntry in trainingDatasetEntries)
            {
                MapInputsToInputNeurons(inputNeurons, trainingEntry.Inputs);
                MapOutputsToOutputNeurons(outputNeurons, trainingEntry.Outputs);
            }
        }

        /// <summary>
        /// Maps each of the <paramref name="inputs"/> to the respective input neuron in <paramref name="inputNeurons"/>.
        /// </summary>
        /// <param name="inputNeurons">The collection of input neurons that the inputs should be mapped to.</param>
        /// <param name="inputs">The network inputs that will be mapped to input neurons.</param>
        private ICollection<IInputNeuron> MapInputsToInputNeurons(ICollection<IInputNeuron> inputNeurons, IEnumerable<INetworkInput> inputs)
        {
            if (inputNeurons == null)
                throw new ArgumentNullException("inputNeurons");

            if (inputs == null)
                throw new ArgumentNullException("inputs");

            if (inputNeurons?.Count() != inputs.Count())
            {
                throw new ArgumentException("The number of network inputs must equal the number of input neurons.");
            }

            // Update each of the network input NeuronIds to coordinate with the input layer neuron neuronIds.
            using (var inputsEnumerator = inputs.GetEnumerator())
            using (var inputNeuronsEnumerator = inputNeurons.GetEnumerator())
            {
                while (inputsEnumerator.MoveNext() && inputNeuronsEnumerator.MoveNext())
                {
                    var networkNeuron = inputNeuronsEnumerator.Current;
                    var networkInput = inputsEnumerator.Current;

                    networkInput.NeuronId = networkNeuron.Id;
                    networkNeuron.Description = networkInput.Description;
                }
            }

            return inputNeurons;
        }

        /// <summary>
        /// Maps each of the <paramref name="outputs"/> to the respective output neuron in <paramref name="outputNeurons"/>.
        /// </summary>
        /// <param name="outputNeurons">The collection of output neurons that the outputs should be mapped to.</param>
        /// <param name="outputs">The network outputs that will be mapped to output neurons.</param>
        private ICollection<IOutputNeuron> MapOutputsToOutputNeurons(ICollection<IOutputNeuron> outputNeurons, IEnumerable<INetworkOutput> outputs)
        {
            if (outputNeurons == null)
                throw new ArgumentNullException("outputNeurons");

            if (outputs == null)
                throw new ArgumentNullException("outputs");

            if (outputNeurons?.Count() != outputs.Count())
            {
                throw new ArgumentException("The number of network outputs must equal the number of output neurons.");
            }

            // Update each of the network output NeuronIds to coordinate with the output layer neuron neuronIds.
            using (var outputsEnumerator = outputs.GetEnumerator())
            using (var outputNeuronsEnumerator = outputNeurons.GetEnumerator())
            {
                while (outputsEnumerator.MoveNext() && outputNeuronsEnumerator.MoveNext())
                {
                    var networkNeuron = outputNeuronsEnumerator.Current;
                    var networkOutput = outputsEnumerator.Current;

                    networkOutput.NeuronId = networkNeuron.Id;
                    networkNeuron.Description = networkOutput.Description;
                }
            }

            return outputNeurons;
        }

        /// <summary>
        /// Maps each of the <paramref name="outputNeurons"/> to the respective <paramref name="outputs"/>.
        /// </summary>
        /// <param name="outputNeurons">The collection of output neurons that will map to the outputs.</param>
        /// <param name="outputs">The network outputs that will be mapped from the output neurons.</param>
        private ICollection<INetworkOutput> MapOutputNeuronsToOutputs(IEnumerable<IOutputNeuron> outputNeurons, ICollection<INetworkOutput> outputs)
        {
            if (outputNeurons == null)
                throw new ArgumentNullException("outputNeurons");

            if (outputs == null)
                throw new ArgumentNullException("outputs");

            if (outputNeurons?.Count() != outputs.Count())
            {
                throw new ArgumentException("The number of network outputs must equal the number of output neurons.");
            }

            // Update each of the network output NeuronIds to coordinate with the output layer neuron neuronIds.
            using (var outputsEnumerator = outputs.GetEnumerator())
            using (var outputNeuronsEnumerator = outputNeurons.GetEnumerator())
            {
                while (outputsEnumerator.MoveNext() && outputNeuronsEnumerator.MoveNext())
                {
                    var networkNeuron = outputNeuronsEnumerator.Current;
                    var networkOutput = outputsEnumerator.Current;

                    networkOutput.NeuronId = networkNeuron.Id;
                    networkOutput.Description = networkNeuron.Description;
                }
            }

            return outputs;
        }

        /// <summary>
        /// Returns the final training date in for the <paramref name="quote"/>.
        /// </summary>
        /// <param name="quote">The quote that the training is applied to.</param>
        /// <returns>Returns the final date that training should end on.</returns>
        private DateTime GetTrainingEndDateForQuote(Q quote)
        {
            return quote.Date.AddDays(-1);
        }

        /// <summary>
        /// Returns the first training date for the <paramref name="quote"/> based on the <paramref name="networkConfig"/>.
        /// </summary>
        /// <param name="quote">The quote that the training is applied to.</param>
        /// <param name="networkConfig">The network configuration that indicates how long to train for.</param>
        /// <returns>Returns the start date that training should begin on.</returns>
        private DateTime GetTrainingStartDateForQuote(Q quote, N networkConfig)
        {
            return quote.Date.AddDays(-1).AddMonths(-1 * networkConfig.NumTrainingMonths);
        }

        /// <summary>
        /// Fetches and applies the necessary network inputs to the network based on the identified network configuration.
        /// </summary>
        /// <param name="network">The neural network that the inputs will be applied to.</param>
        /// <param name="networkConfig">The NetworkConfiguration to pull inputs from.</param>
        /// <param name="companyId">The id of the company that the inputs are chosen for.</param>
        /// <param name="testDate">The date of the calculated inputs that will be applied to the <paramref name="network"/>.</param>
        /// <returns>Returns the network outputs after the config inputs have been applied.</returns>
        private IEnumerable<INetworkOutput> ApplyConfigInputsToNetwork(IDFFNeuralNetwork network, N networkConfig, int companyId, DateTime testDate)
        {
            var networkInputLayer = network.Layers?.OfType<IInputLayer>().First();
            var inputNeurons = networkInputLayer.Neurons?.OfType<IInputNeuron>().ToList();
            var networkInputs = _datasetService.GetNetworkInputs(networkConfig.DatasetRetrievalMethodId, companyId, testDate);

            MapInputsToInputNeurons(inputNeurons, networkInputs);

            // Apply inputs to network and returns network outputs.
            var resultingNetworkOutputs = network.ApplyInputs(networkInputs).ToList();

            var networkOutputLayer = network.Layers?.OfType<IOutputLayer>().First();
            var outputNeurons = networkOutputLayer.Neurons?.OfType<IOutputNeuron>();

            return MapOutputNeuronsToOutputs(outputNeurons, resultingNetworkOutputs);
        }

        /// <summary>
        /// 
        /// </summary>
        public void AnalyzePredictions()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("PredictionService", "The service has been disposed.");

            var quotes = _quoteService.GetQuotes();

            // Filter out predictions that are invalid, have ActualOutcomes already, contain invalid quoteIds, and those that have less that 5 remaining quotes for the company. 
            foreach (var prediction in GetPredictions().Where(p => p != null && (p.ActualOutcome == null || p.ActualOutcome == string.Empty)))
            {

                // TODO: Revisit this...
                var existingQuote = quotes.FirstOrDefault(q => q.Id == prediction.Id);
                if (existingQuote != null && quotes.Count(q => q.CompanyId == existingQuote.CompanyId && q.Id > prediction.QuoteId) >= 5)
                {
                    // Update actual outcome for each prediction.
                    UpdatePredictionWithActualOutcome(prediction, existingQuote);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prediction"></param>
        /// <param name="quote"></param>
        private void UpdatePredictionWithActualOutcome(P prediction, Q quote)
        {
            if (prediction == null)
                throw new ArgumentNullException("prediction");

            if (quote == null)
                throw new ArgumentNullException("quote");

            var actualOutcome = _datasetService.GetExpectedNetworkOutputs(prediction.NetworkConfiguration.DatasetRetrievalMethodId, quote.CompanyId, quote.Date);

            prediction.ActualOutcome = string.Join(",", actualOutcome.Select(o => string.Format(PREDICTION_DESCRIPTION_FORMAT, o.ActivationLevel, o.Description)));
            Update(prediction);
        }

        #endregion
        #region IDisposable
        /// <summary>
        /// Disposes this object and properly cleans up resources. 
        /// </summary>
        protected void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _predictionRepository.Dispose();
                    _quoteService.Dispose();
                    _companyService.Dispose();
                    _datasetService.Dispose();
                    _networkConfigurationService.Dispose();
                }

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Disposes this object and properly cleans up resources. 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}