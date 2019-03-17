# Market-Analysis

Various experiments using share market data


**15 years of ASX 300 converted to audio**
![test.wav](https://github.com/Bencargs/Market-Analysis/blob/master/test.wav)


**Probability Distrobution Function of ASX 300**
![ASX300 Price Histogram (gradient)](https://i.imgur.com/mRwMxwl.png)


**Probability Distrobution Function of ASX 300 (Colour Scale)**
![ASX300 Price Histogram (colour scale)](https://i.imgur.com/8QDsMJx.png)


## Details
Data is generated by taking the average Mean and Histograms of price movements for each day over a
reference ‘window’ using the 15 years of the ASX 300.

Each day of the dataset was categorised in their relevant window, and price and
frequencies calculated.
The values for the daily histogram for each reference window can than be charted, with price spread
on the Y axis, and day of the year on the X, and frequency of price point represented by the intensity
of the colour. As below:
![Reference Windows Probability Distrobution Frequencies](https://i.imgur.com/SJ4GpyL.png)

The trends are then combined into a single image. Frequencies are intensified die to constructive interference.

**Random Walk on ASX 300**
![Random Walk on ASX 300](https://i.imgur.com/3hOHEWH.png)

Using the above PDF, a random walk simulator was constructed to reveal broard statistical trends and averages.
