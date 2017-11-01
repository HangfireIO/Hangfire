﻿using System;
using Hangfire.Common;
using Hangfire.States;
using Xunit;

namespace Hangfire.Core.Tests.States
{
    public class ManualStateFacts
    {
        [Fact]
        public void StateName_IsCorrect()
        {
            var state = new ManualState();
            Assert.Equal(ManualState.StateName, state.Name);
        }

        [Fact]
        public void Ctor_ShouldSetQueue_WhenItWasGiven()
        {
            var state = new ManualState("critical");
            Assert.Equal("critical", state.Queue);
        }

        [Fact]
        public void SetQueue_ThrowsAnException_WhenQueueValueIsEmpty()
        {
            var state = new ManualState();
            Assert.Throws<ArgumentNullException>(() => state.Queue = String.Empty);
        }

        [Fact]
        public void SetQueue_ThrowsAnException_WhenValueIsNotInAGivenFormat()
        {
            var state = new ManualState();

            Assert.Throws<ArgumentException>(() => state.Queue = "UppercaseLetters");
            Assert.Throws<ArgumentException>(() => state.Queue = "punctuation:un-allowed");
            Assert.Throws<ArgumentException>(() => state.Queue = "моя_твоя_непонимать");
        }

        [Fact]
        public void SetQueue_DoesNotThrowException_WhenValueIsInACorrectFormat()
        {
            var state = new ManualState();

            // Does not throw
            state.Queue = "lowercasedcharacters";
            state.Queue = "underscores_allowed";
            state.Queue = "1234567890_allowed";
        }

        [Fact]
        public void SerializeData_ReturnsCorrectData()
        {
            var state = new ManualState();

            var serializedData = state.SerializeData();

            Assert.Equal(state.Queue, serializedData["Queue"]);
            Assert.Equal(JobHelper.SerializeDateTime(state.CreatedAt), serializedData["CreatedAt"]);
        }

        [Fact]
        public void IsFinal_ReturnsFalse()
        {
            var state = new ManualState();

            Assert.False(state.IsFinal);
        }

        [Fact]
        public void IgnoreExceptions_ReturnsFalse()
        {
            var state = new ManualState();

            Assert.False(state.IgnoreJobLoadException);
        }
    }
}
