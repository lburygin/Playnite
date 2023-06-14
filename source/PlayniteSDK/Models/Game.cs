using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Playnite.SDK.Data;

namespace Playnite.SDK.Models
{


    /// <summary>
    /// Represents Playnite game object.
    /// </summary>
    public class Game : DatabaseObject
    {
        /// <summary>
        /// Gets the most recent date between the last played and added dates.
        /// </summary>
        /// <returns></returns>
        public DateTime? GetGameRecentActivity()
        {
            if (lastActivity == null)
            {
                return added;
            }
            else if (added == null || lastActivity > added)
            {
                return lastActivity;
            }
            else
            {
                return added;
            }
        }

        /// <summary>
        /// Gets play time category.
        /// </summary>
        /// <param name="seconds">Play time in seconds.</param>
        /// <returns></returns>
        private PlaytimeCategory GetPlayTimeCategory(ulong seconds)
        {
            if (seconds == 0)
            {
                return PlaytimeCategory.NotPlayed;
            }

            var hours = seconds / 3600;
            if (hours < 1)
            {
                return PlaytimeCategory.LessThenHour;
            }
            else if (hours >= 1 && hours <= 10)
            {
                return PlaytimeCategory.O1_10;
            }
            else if (hours >= 10 && hours <= 100)
            {
                return PlaytimeCategory.O10_100;
            }
            else if (hours >= 100 && hours <= 500)
            {
                return PlaytimeCategory.O100_500;
            }
            else if (hours >= 500 && hours <= 1000)
            {
                return PlaytimeCategory.O500_1000;
            }
            else
            {
                return PlaytimeCategory.O1000plus;
            }
        }

        /// <summary>
        /// Gets time segment.
        /// </summary>
        /// <param name="dateTime">Date time to be measured.</param>
        /// <returns></returns>
        private PastTimeSegment GetPastTimeSegment(DateTime? dateTime)
        {
            if (dateTime == null)
            {
                return PastTimeSegment.Never;
            }

            if (dateTime.Value.Date == DateTime.Today)
            {
                return PastTimeSegment.Today;
            }

            if (dateTime.Value.Date.AddDays(1) == DateTime.Today)
            {
                return PastTimeSegment.Yesterday;
            }

            var diff = DateTime.Now - dateTime.Value;
            if (diff.TotalDays < 7)
            {
                return PastTimeSegment.PastWeek;
            }

            if (diff.TotalDays < 31)
            {
                return PastTimeSegment.PastMonth;
            }

            if (diff.TotalDays < 365)
            {
                return PastTimeSegment.PastYear;
            }

            return PastTimeSegment.MoreThenYear;
        }

        /// <summary>
        /// Gets score rating.
        /// </summary>
        /// <param name="score">Score.</param>
        /// <returns></returns>
        private ScoreRating GetScoreRating(int? score)
        {
            if (score == null)
            {
                return ScoreRating.None;
            }
            else if (score > 75)
            {
                return ScoreRating.Positive;
            }
            else if (score > 25)
            {
                return ScoreRating.Mixed;
            }
            else
            {
                return ScoreRating.Negative;
            }
        }

        /// <summary>
        /// Gets score group.
        /// </summary>
        /// <param name="score">Score.</param>
        /// <returns></returns>
        private ScoreGroup GetScoreGroup(int? score)
        {
            if (score >= 0 && score < 10)
            {
                return ScoreGroup.O0x;
            }

            if (score >= 10 && score < 20)
            {
                return ScoreGroup.O1x;
            }

            if (score >= 20 && score < 30)
            {
                return ScoreGroup.O2x;
            }

            if (score >= 30 && score < 40)
            {
                return ScoreGroup.O3x;
            }

            if (score >= 40 && score < 50)
            {
                return ScoreGroup.O4x;
            }

            if (score >= 50 && score < 60)
            {
                return ScoreGroup.O5x;
            }

            if (score >= 60 && score < 70)
            {
                return ScoreGroup.O6x;
            }

            if (score >= 70 && score < 80)
            {
                return ScoreGroup.O7x;
            }

            if (score >= 80 && score < 90)
            {
                return ScoreGroup.O8x;
            }

            if (score >= 90)
            {
                return ScoreGroup.O9x;
            }

            return ScoreGroup.None;
        }

        /// <summary>
        /// Gets or sets game database reference.
        /// </summary>
        internal static IGameDatabase DatabaseReference
        {
            get; set;
        }

        /// <summary>
        /// Creates new instance of a Game object.
        /// </summary>
        public Game() : base()
        {
            GameId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Creates new instance of a Game object with specific name.
        /// </summary>
        /// <param name="name">Game name.</param>
        public Game(string name) : this()
        {
            Name = name;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public Game GetCopy()
        {
            return new Game
            {
                Id = Id,
                GameId = GameId,
                PluginId = PluginId,
                Name = Name,
                Icon = Icon,
                CoverImage = CoverImage,
                BackgroundImage = BackgroundImage,
                Description = Description,
                Notes = Notes,
                EnableSystemHdr = EnableSystemHdr,
                Hidden = Hidden,
                Favorite = Favorite,
                InstallDirectory = InstallDirectory,
                LastActivity = LastActivity,
                SortingName = SortingName,
                ReleaseDate = ReleaseDate,
                IsInstalled = IsInstalled,
                IsInstalling = IsInstalling,
                IsLaunching = IsLaunching,
                IsUninstalling = IsUninstalling,
                IsRunning = IsRunning,
                Playtime = Playtime,
                Added = Added,
                Modified = Modified,
                PlayCount = PlayCount,
                InstallSize = InstallSize,
                LastSizeScanDate = LastSizeScanDate,
                Version = Version,
                GenreIds = GenreIds?.ToList(),
                PlatformIds = PlatformIds?.ToList(),
                PublisherIds = PublisherIds?.ToList(),
                DeveloperIds = DeveloperIds?.ToList(),
                CategoryIds = CategoryIds?.ToList(),
                TagIds = TagIds?.ToList(),
                FeatureIds = FeatureIds?.ToList(),
                SeriesIds = SeriesIds?.ToList(),
                AgeRatingIds = AgeRatingIds?.ToList(),
                RegionIds = RegionIds?.ToList(),
                SourceId = SourceId,
                CompletionStatusId = CompletionStatusId,
                UserScore = UserScore,
                CriticScore = CriticScore,
                CommunityScore = CommunityScore,
                PreScript = PreScript,
                PostScript = PostScript,
                GameStartedScript = GameStartedScript,
                UseGlobalPostScript = UseGlobalPostScript,
                UseGlobalPreScript = UseGlobalPreScript,
                UseGlobalGameStartedScript = UseGlobalGameStartedScript,
                Manual = Manual,
                IncludeLibraryPluginAction = IncludeLibraryPluginAction,
                OverrideInstallState = OverrideInstallState,
                GameActions = GameActions?.Select(a => a.GetCopy()).ToObservable(),
                Links = Links?.Select(a => a.GetCopy()).ToObservable(),
                Roms = Roms?.Select(a => a.GetCopy()).ToObservable(),
            };
        }

        /// <inheritdoc/>
        public override void CopyDiffTo(object target)
        {
            base.CopyDiffTo(target);

            if (target is Game tro)
            {
                if (!string.Equals(BackgroundImage, tro.BackgroundImage, StringComparison.Ordinal))
                {
                    tro.BackgroundImage = BackgroundImage;
                }

                if (!string.Equals(Description, tro.Description, StringComparison.Ordinal))
                {
                    tro.Description = Description;
                }

                if (!string.Equals(Notes, tro.Notes, StringComparison.Ordinal))
                {
                    tro.Notes = Notes;
                }

                if (!GenreIds.IsListEqual(tro.GenreIds))
                {
                    tro.GenreIds = GenreIds;
                }

                if (EnableSystemHdr != tro.EnableSystemHdr)
                {
                    tro.EnableSystemHdr = EnableSystemHdr;
                }

                if (Hidden != tro.Hidden)
                {
                    tro.Hidden = Hidden;
                }

                if (Favorite != tro.Favorite)
                {
                    tro.Favorite = Favorite;
                }

                if (!string.Equals(Icon, tro.Icon, StringComparison.Ordinal))
                {
                    tro.Icon = Icon;
                }

                if (!string.Equals(CoverImage, tro.CoverImage, StringComparison.Ordinal))
                {
                    tro.CoverImage = CoverImage;
                }

                if (!string.Equals(InstallDirectory, tro.InstallDirectory, StringComparison.Ordinal))
                {
                    tro.InstallDirectory = InstallDirectory;
                }

                if (LastActivity != tro.LastActivity)
                {
                    tro.LastActivity = LastActivity;
                }

                if (!string.Equals(SortingName, tro.SortingName, StringComparison.Ordinal))
                {
                    tro.SortingName = SortingName;
                }

                if (!string.Equals(GameId, tro.GameId, StringComparison.Ordinal))
                {
                    tro.GameId = GameId;
                }

                if (PluginId != tro.PluginId)
                {
                    tro.PluginId = PluginId;
                }

                if (!GameActions.IsListEqualExact(tro.GameActions))
                {
                    tro.GameActions = GameActions;
                }

                if (!PlatformIds.IsListEqual(tro.PlatformIds))
                {
                    tro.PlatformIds = PlatformIds;
                }

                if (!PublisherIds.IsListEqual(tro.PublisherIds))
                {
                    tro.PublisherIds = PublisherIds;
                }

                if (!DeveloperIds.IsListEqual(tro.DeveloperIds))
                {
                    tro.DeveloperIds = DeveloperIds;
                }

                if (ReleaseDate != tro.ReleaseDate)
                {
                    tro.ReleaseDate = ReleaseDate;
                }

                if (!CategoryIds.IsListEqual(tro.CategoryIds))
                {
                    tro.CategoryIds = CategoryIds;
                }

                if (!TagIds.IsListEqual(tro.TagIds))
                {
                    tro.TagIds = TagIds;
                }

                if (!FeatureIds.IsListEqual(tro.FeatureIds))
                {
                    tro.FeatureIds = FeatureIds;
                }

                if (!Links.IsListEqualExact(tro.Links))
                {
                    tro.Links = Links;
                }

                if (!Roms.IsListEqualExact(tro.Roms))
                {
                    tro.Roms = Roms;
                }

                if (IsInstalling != tro.IsInstalling)
                {
                    tro.IsInstalling = IsInstalling;
                }

                if (IsUninstalling != tro.IsUninstalling)
                {
                    tro.IsUninstalling = IsUninstalling;
                }

                if (IsLaunching != tro.IsLaunching)
                {
                    tro.IsLaunching = IsLaunching;
                }

                if (IsRunning != tro.IsRunning)
                {
                    tro.IsRunning = IsRunning;
                }

                if (IsInstalled != tro.IsInstalled)
                {
                    tro.IsInstalled = IsInstalled;
                }

                if (Playtime != tro.Playtime)
                {
                    tro.Playtime = Playtime;
                }

                if (Added != tro.Added)
                {
                    tro.Added = Added;
                }

                if (Modified != tro.Modified)
                {
                    tro.Modified = Modified;
                }

                if (PlayCount != tro.PlayCount)
                {
                    tro.PlayCount = PlayCount;
                }

                if (InstallSize != tro.InstallSize)
                {
                    tro.InstallSize = InstallSize;
                }

                if (LastSizeScanDate != tro.lastSizeScanDate)
                {
                    tro.LastSizeScanDate = LastSizeScanDate;
                }

                if (!SeriesIds.IsListEqual(tro.SeriesIds))
                {
                    tro.SeriesIds = SeriesIds;
                }

                if (Version != tro.Version)
                {
                    tro.Version = Version;
                }

                if (!AgeRatingIds.IsListEqual(tro.AgeRatingIds))
                {
                    tro.AgeRatingIds = AgeRatingIds;
                }

                if (!RegionIds.IsListEqual(tro.RegionIds))
                {
                    tro.RegionIds = RegionIds;
                }

                if (SourceId != tro.SourceId)
                {
                    tro.SourceId = SourceId;
                }

                if (CompletionStatusId != tro.CompletionStatusId)
                {
                    tro.CompletionStatusId = CompletionStatusId;
                }

                if (UserScore != tro.UserScore)
                {
                    tro.UserScore = UserScore;
                }

                if (CriticScore != tro.CriticScore)
                {
                    tro.CriticScore = CriticScore;
                }

                if (CommunityScore != tro.CommunityScore)
                {
                    tro.CommunityScore = CommunityScore;
                }

                if (!string.Equals(PreScript, tro.PreScript, StringComparison.Ordinal))
                {
                    tro.PreScript = PreScript;
                }

                if (!string.Equals(PostScript, tro.PostScript, StringComparison.Ordinal))
                {
                    tro.PostScript = PostScript;
                }

                if (!string.Equals(GameStartedScript, tro.GameStartedScript, StringComparison.Ordinal))
                {
                    tro.GameStartedScript = GameStartedScript;
                }

                if (UseGlobalPostScript != tro.UseGlobalPostScript)
                {
                    tro.UseGlobalPostScript = UseGlobalPostScript;
                }

                if (UseGlobalPreScript != tro.UseGlobalPreScript)
                {
                    tro.UseGlobalPreScript = UseGlobalPreScript;
                }

                if (UseGlobalGameStartedScript != tro.UseGlobalGameStartedScript)
                {
                    tro.UseGlobalGameStartedScript = UseGlobalGameStartedScript;
                }

                if (!string.Equals(Manual, tro.Manual, StringComparison.Ordinal))
                {
                    tro.Manual = Manual;
                }

                if (IncludeLibraryPluginAction != tro.IncludeLibraryPluginAction)
                {
                    tro.IncludeLibraryPluginAction = IncludeLibraryPluginAction;
                }

                if (OverrideInstallState != tro.OverrideInstallState)
                {
                    tro.OverrideInstallState = OverrideInstallState;
                }
            }
            else
            {
                throw new ArgumentException($"Target object has to be of type {GetType().Name}");
            }
        }

        /// <summary>
        /// Gets differences in game objects.
        /// </summary>
        /// <param name="otherGame">Game object to compare to.</param>
        /// <returns>List of fields that differ.</returns>
        public List<GameField> GetDifferences(Game otherGame)
        {
            var changes = new List<GameField>();
            if (!string.Equals(BackgroundImage, otherGame.BackgroundImage, StringComparison.Ordinal))
            {
                changes.Add(GameField.BackgroundImage);
            }

            if (!string.Equals(Description, otherGame.Description, StringComparison.Ordinal))
            {
                changes.Add(GameField.Description);
            }

            if (!string.Equals(Notes, otherGame.Notes, StringComparison.Ordinal))
            {
                changes.Add(GameField.Notes);
            }

            if (!GenreIds.IsListEqual(otherGame.GenreIds))
            {
                changes.Add(GameField.GenreIds);
                changes.Add(GameField.Genres);
            }

            if (EnableSystemHdr != otherGame.enableSystemHdr)
            {
                changes.Add(GameField.EnableSystemHdr);
            }

            if (Hidden != otherGame.Hidden)
            {
                changes.Add(GameField.Hidden);
            }

            if (Favorite != otherGame.Favorite)
            {
                changes.Add(GameField.Favorite);
            }

            if (!string.Equals(Icon, otherGame.Icon, StringComparison.Ordinal))
            {
                changes.Add(GameField.Icon);
            }

            if (!string.Equals(CoverImage, otherGame.CoverImage, StringComparison.Ordinal))
            {
                changes.Add(GameField.CoverImage);
            }

            if (!string.Equals(InstallDirectory, otherGame.InstallDirectory, StringComparison.Ordinal))
            {
                changes.Add(GameField.InstallDirectory);
            }

            if (LastActivity != otherGame.LastActivity)
            {
                changes.Add(GameField.LastActivity);
                if (LastActivitySegment != otherGame.LastActivitySegment)
                {
                    changes.Add(GameField.LastActivitySegment);
                }
            }

            if (!string.Equals(SortingName, otherGame.SortingName, StringComparison.Ordinal))
            {
                changes.Add(GameField.SortingName);
            }

            if (!string.Equals(GameId, otherGame.GameId, StringComparison.Ordinal))
            {
                changes.Add(GameField.Gameid);
            }

            if (PluginId != otherGame.PluginId)
            {
                changes.Add(GameField.PluginId);
            }

            if (!GameActions.IsListEqualExact(otherGame.GameActions))
            {
                changes.Add(GameField.GameActions);
            }

            if (!PlatformIds.IsListEqual(otherGame.PlatformIds))
            {
                changes.Add(GameField.PlatformIds);
                changes.Add(GameField.Platforms);
            }

            if (!PublisherIds.IsListEqual(otherGame.PublisherIds))
            {
                changes.Add(GameField.PublisherIds);
                changes.Add(GameField.Publishers);
            }

            if (!DeveloperIds.IsListEqual(otherGame.DeveloperIds))
            {
                changes.Add(GameField.DeveloperIds);
                changes.Add(GameField.Developers);
            }

            if (ReleaseDate != otherGame.ReleaseDate)
            {
                changes.Add(GameField.ReleaseDate);
                if (ReleaseYear != otherGame.ReleaseYear)
                {
                    changes.Add(GameField.ReleaseYear);
                }
            }

            if (!CategoryIds.IsListEqual(otherGame.CategoryIds))
            {
                changes.Add(GameField.CategoryIds);
                changes.Add(GameField.Categories);
            }

            if (!TagIds.IsListEqual(otherGame.TagIds))
            {
                changes.Add(GameField.TagIds);
                changes.Add(GameField.Tags);
            }

            if (!FeatureIds.IsListEqual(otherGame.FeatureIds))
            {
                changes.Add(GameField.FeatureIds);
                changes.Add(GameField.Features);
            }

            if (!Links.IsListEqualExact(otherGame.Links))
            {
                changes.Add(GameField.Links);
            }

            if (!Roms.IsListEqualExact(otherGame.Roms))
            {
                changes.Add(GameField.Roms);
            }

            if (IsInstalling != otherGame.IsInstalling)
            {
                changes.Add(GameField.IsInstalling);
            }

            if (IsUninstalling != otherGame.IsUninstalling)
            {
                changes.Add(GameField.IsUninstalling);
            }

            if (IsLaunching != otherGame.IsLaunching)
            {
                changes.Add(GameField.IsLaunching);
            }

            if (IsRunning != otherGame.IsRunning)
            {
                changes.Add(GameField.IsRunning);
            }

            if (IsInstalled != otherGame.IsInstalled)
            {
                changes.Add(GameField.IsInstalled);
                changes.Add(GameField.InstallationStatus);
            }

            if (Playtime != otherGame.Playtime)
            {
                changes.Add(GameField.Playtime);
                if (PlaytimeCategory != otherGame.PlaytimeCategory)
                {
                    changes.Add(GameField.PlaytimeCategory);
                }
            }

            if (Added != otherGame.Added)
            {
                changes.Add(GameField.Added);
                if (AddedSegment != otherGame.AddedSegment)
                {
                    changes.Add(GameField.AddedSegment);
                }
            }

            if (Modified != otherGame.Modified)
            {
                changes.Add(GameField.Modified);
                if (Modified != otherGame.Modified)
                {
                    changes.Add(GameField.ModifiedSegment);
                }
            }

            if (PlayCount != otherGame.PlayCount)
            {
                changes.Add(GameField.PlayCount);
            }

            if (InstallSize != otherGame.InstallSize)
            {
                changes.Add(GameField.InstallSize);
            }

            if (LastSizeScanDate != otherGame.LastSizeScanDate)
            {
                changes.Add(GameField.LastSizeScanDate);
            }

            if (!SeriesIds.IsListEqual(otherGame.SeriesIds))
            {
                changes.Add(GameField.SeriesIds);
                changes.Add(GameField.Series);
            }

            if (Version != otherGame.Version)
            {
                changes.Add(GameField.Version);
            }

            if (!AgeRatingIds.IsListEqual(otherGame.AgeRatingIds))
            {
                changes.Add(GameField.AgeRatingIds);
                changes.Add(GameField.AgeRatings);
            }

            if (!RegionIds.IsListEqual(otherGame.RegionIds))
            {
                changes.Add(GameField.RegionIds);
                changes.Add(GameField.Regions);
            }

            if (SourceId != otherGame.SourceId)
            {
                changes.Add(GameField.SourceId);
                changes.Add(GameField.Source);
            }

            if (CompletionStatusId != otherGame.CompletionStatusId)
            {
                changes.Add(GameField.CompletionStatusId);
                changes.Add(GameField.CompletionStatus);
            }

            if (UserScore != otherGame.UserScore)
            {
                changes.Add(GameField.UserScore);
                if (UserScoreGroup != otherGame.UserScoreGroup)
                {
                    changes.Add(GameField.UserScoreGroup);
                }

                if (UserScoreRating != otherGame.UserScoreRating)
                {
                    changes.Add(GameField.UserScoreRating);
                }
            }

            if (CriticScore != otherGame.CriticScore)
            {
                changes.Add(GameField.CriticScore);
                if (CriticScoreGroup != otherGame.CriticScoreGroup)
                {
                    changes.Add(GameField.CriticScoreGroup);
                }

                if (CriticScoreRating != otherGame.CriticScoreRating)
                {
                    changes.Add(GameField.CriticScoreRating);
                }
            }

            if (CommunityScore != otherGame.CommunityScore)
            {
                changes.Add(GameField.CommunityScore);
                if (CommunityScoreGroup != otherGame.CommunityScoreGroup)
                {
                    changes.Add(GameField.CommunityScoreGroup);
                }

                if (CommunityScoreRating != otherGame.CommunityScoreRating)
                {
                    changes.Add(GameField.CommunityScoreRating);
                }
            }

            if (!string.Equals(PreScript, otherGame.PreScript, StringComparison.Ordinal))
            {
                changes.Add(GameField.PreScript);
            }

            if (!string.Equals(PostScript, otherGame.PostScript, StringComparison.Ordinal))
            {
                changes.Add(GameField.PostScript);
            }

            if (!string.Equals(GameStartedScript, otherGame.GameStartedScript, StringComparison.Ordinal))
            {
                changes.Add(GameField.GameStartedScript);
            }

            if (UseGlobalPostScript != otherGame.UseGlobalPostScript)
            {
                changes.Add(GameField.UseGlobalPostScript);
            }

            if (UseGlobalPreScript != otherGame.UseGlobalPreScript)
            {
                changes.Add(GameField.UseGlobalPreScript);
            }

            if (UseGlobalGameStartedScript != otherGame.UseGlobalGameStartedScript)
            {
                changes.Add(GameField.UseGlobalGameStartedScript);
            }

            if (!string.Equals(Manual, otherGame.Manual, StringComparison.Ordinal))
            {
                changes.Add(GameField.Manual);
            }

            if (IncludeLibraryPluginAction != otherGame.IncludeLibraryPluginAction)
            {
                changes.Add(GameField.IncludeLibraryPluginAction);
            }

            if (OverrideInstallState != otherGame.OverrideInstallState)
            {
                changes.Add(GameField.OverrideInstallState);
            }

            return changes;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public char GetNameGroup()
        {
            var nameMatch = string.IsNullOrEmpty(SortingName) ? Name : SortingName;
            if (string.IsNullOrEmpty(nameMatch))
            {
                return '#';
            }
            else
            {
                var firstChar = char.ToUpper(nameMatch[0]);
                return char.IsLetter(firstChar) ? firstChar : '#';
            }
        }

        /// <summary>
        /// Gets game Install Size group.
        /// </summary>
        public InstallSizeGroup GetInstallSizeGroup()
        {
            if (installSize == null || installSize == 0)
            {
                return InstallSizeGroup.None;
            }
            else if (installSize <= 0x6400000) //100MB
            {
                return InstallSizeGroup.S0_0MB_100MB;
            }
            else if (installSize <= 0x40000000) //1GB
            {
                return InstallSizeGroup.S1_100MB_1GB;
            }
            else if (installSize <= 0x140000000) //5GB
            {
                return InstallSizeGroup.S2_1GB_5GB;
            }
            else if (installSize <= 0x280000000) //10GB
            {
                return InstallSizeGroup.S3_5GB_10GB;
            }
            else if (installSize <= 0x500000000) //20GB
            {
                return InstallSizeGroup.S4_10GB_20GB;
            }
            else if (installSize <= 0xA00000000) //40GB
            {
                return InstallSizeGroup.S5_20GB_40GB;
            }
            else if (installSize <= 0x1900000000) //100GB
            {
                return InstallSizeGroup.S6_40GB_100GB;
            }

            return InstallSizeGroup.S7_100GBPlus;
        }

        /// <summary>
        /// Gets game Install Drive group.
        /// </summary>
        public string GetInstallDriveGroup()
        {
            var installDrive = GetInstallDrive();
            if (string.IsNullOrEmpty(installDrive))
            {
                return ResourceProvider.GetString("LOCNone");
            }
            else
            {
                return installDrive;
            }
        }

        /// <summary>
        /// Gets game Install Drive.
        /// </summary>
        public string GetInstallDrive()
        {
            if (!isInstalled)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(InstallDirectory))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetPathRoot(InstallDirectory).ToUpperInvariant();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
