using System.Reflection;
using System.Collections.Generic;
using Xunit;

namespace YAFC.Model.Tests
{
    public class MilestonesTests
    {
        private static Milestones setupMilestones(ulong result, ulong mask, out FactorioObject factorioObj)
        {
            factorioObj = new Technology();
            var milestoneResult = new Mapping<FactorioObject, ulong>(new FactorioIdRange<FactorioObject>(0, 1, new List<FactorioObject>() {
                factorioObj
            }));
            milestoneResult[factorioObj] = result;

            var milestones = new Milestones()
            {
                milestoneResult = milestoneResult,
                currentMilestones = new FactorioObject[] { factorioObj }
            };

            var milestonesType = typeof(Milestones);
            var milestonesLockedMask = milestonesType.GetProperty("lockedMask");
            milestonesLockedMask.SetValue(milestones, mask);

            return milestones;
        }

        [Theory]
        [InlineData(0, 1, false)]
        [InlineData(1, 0, false)]
        [InlineData(1, 1, true)]
        [InlineData(3, 1, true)]
        [InlineData(1, 3, true)]
        public void IsAccessibleWithCurrentMilestones_WhenGivenMilestones_ShouldReturnCorrectValue(ulong result, ulong mask, bool expectedResult)
        {
            var milestones = setupMilestones(result, mask, out FactorioObject factorioObj);

            Assert.Equal(expectedResult, milestones.IsAccessibleWithCurrentMilestones(0));
            Assert.Equal(expectedResult, milestones.IsAccessibleWithCurrentMilestones(factorioObj));
        }

        [Theory]
        [InlineData(1, 1, true)]
        [InlineData(3, 3, false)]
        [InlineData(15, 15, false)] // Triggers last return
        [InlineData(16, 16, false)] // Triggers last return
        [InlineData(17, 17, false)] // Caught by 'bit 0 check', otherwise last return would return true
        public void IsAccessibleAtNextMilestone_WhenGivenMilestones_ShouldReturnCorrectValue(ulong result, ulong mask, bool expectedResult)
        {
            var milestones = setupMilestones(result, mask, out FactorioObject factorioObj);

            Assert.Equal(expectedResult, milestones.IsAccessibleAtNextMilestone(factorioObj));
        }

        [Theory]
        [InlineData(false, ~0ul)] // all bits set (nothing gets masked)
        [InlineData(true, ~0ul & ~2ul)] // all bits set, except bit 1 (for reasons not bit 0, even if the FIRST milestone has its flag set?!)
        public void GetLockedMaskFromProject_ShouldCalculateMask(bool unlocked, ulong expectedResult)
        {
            var milestonesType = typeof(Milestones);
            var getLockedMaskFromProject = milestonesType.GetMethod("GetLockedMaskFromProject", BindingFlags.NonPublic | BindingFlags.Instance);
            var projectField = milestonesType.GetField("project", BindingFlags.NonPublic | BindingFlags.Instance);

            var milestones = setupMilestones(0, 0, out FactorioObject factorioObj);

            var project = new Project();
            if (unlocked)
            {
                // Can't use SetFlag() as it uses the Undo system, which requires SDL
                var flags = project.settings.itemFlags;
                flags[factorioObj] = ProjectPerItemFlags.MilestoneUnlocked;
                var projectType = typeof(ProjectSettings);
                var itemFlagsField = projectType.GetField("<itemFlags>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                itemFlagsField.SetValue(project.settings, flags);
            }


            projectField.SetValue(milestones, project);

            getLockedMaskFromProject.Invoke(milestones, null);

            Assert.Equal(expectedResult, milestones.lockedMask);
        }

        [Theory]
        [InlineData(1, 0, true, false)]  // HighestBitSet() - 1, so bit 0 is never in range...
        [InlineData(2, 0, true, true)] // mask is ignored -> true
        [InlineData(2, 0, false, false)] // mask is active -> false
        [InlineData(2, 2, true, true)]
        [InlineData(2, 2, false, true)] // mask is active and overlaps -> true
        [InlineData(4, 0, true, false)]  // HighestBitSet() too large...
        public void GetHighest_WhenGivenMilestones_ShouldReturnCorrectValue(ulong result, ulong mask, bool all, bool expectObject)
        {
            var milestones = setupMilestones(result, mask, out FactorioObject factorioObj);

            if (expectObject)
            {
                Assert.Equal(factorioObj, milestones.GetHighest(factorioObj, all));
            }
            else
            {
                Assert.Null(milestones.GetHighest(factorioObj, all));
            }
        }
    }
}