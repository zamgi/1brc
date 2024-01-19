# 1️⃣🐝🏎️ The One Billion Row Challenge

.NET/C# impl of https://github.com/gunnarmorling/1brc

# Results

test file on HDD.

first start on cold-started phicical machine (with empty disk cache):

- Total row count: 1 000 000 000
- Total elapsed: 00:01:47.2725263

next starts on same phicical machine (with non-empty disk cache):
- Total row count: 1 000 000 000
- Total elapsed: 00:00:02.6291879

util for reset/clear disk cache on Windows (for getting a truly true measurement of time): https://github.com/zamgi/ReleaseStandbyMemoryPages
