-- =============================================
-- Loyalty Points Table Creation Script
-- =============================================

-- Create LoyaltyPoints table if it doesn't exist
CREATE TABLE IF NOT EXISTS `LoyaltyPoints` (
    `Id` CHAR(36) NOT NULL PRIMARY KEY,
    `CustomerId` VARCHAR(255) NOT NULL,
    `Points` INT NOT NULL,
    `EarnedDate` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `Description` VARCHAR(500) NOT NULL,
    CONSTRAINT `FK_LoyaltyPoints_AspNetUsers_CustomerId` 
        FOREIGN KEY (`CustomerId`) REFERENCES `AspNetUsers`(`Id`) ON DELETE CASCADE,
    INDEX `IX_LoyaltyPoints_CustomerId` (`CustomerId`),
    INDEX `IX_LoyaltyPoints_EarnedDate` (`EarnedDate`)
);

-- =============================================
-- Example SQL Queries for Loyalty Points
-- =============================================

-- 1. Calculate a user's total loyalty point balance
SELECT 
    u.UserName,
    u.Email,
    COALESCE(SUM(lp.Points), 0) as TotalPoints,
    COALESCE(SUM(CASE WHEN lp.Points > 0 THEN lp.Points ELSE 0 END), 0) as EarnedPoints,
    COALESCE(SUM(CASE WHEN lp.Points < 0 THEN ABS(lp.Points) ELSE 0 END), 0) as RedeemedPoints
FROM AspNetUsers u
LEFT JOIN LoyaltyPoints lp ON u.Id = lp.CustomerId
WHERE u.Id = 'USER_ID_HERE'
GROUP BY u.Id, u.UserName, u.Email;

-- 2. Get loyalty points history for a user
SELECT 
    lp.Id,
    lp.Points,
    lp.Description,
    lp.EarnedDate,
    CASE 
        WHEN lp.Points > 0 THEN 'Earned'
        ELSE 'Redeemed'
    END as TransactionType
FROM LoyaltyPoints lp
WHERE lp.CustomerId = 'USER_ID_HERE'
ORDER BY lp.EarnedDate DESC;

-- 3. Get top customers by loyalty points
SELECT 
    u.UserName,
    u.Email,
    SUM(lp.Points) as TotalPoints,
    COUNT(lp.Id) as TransactionCount
FROM AspNetUsers u
INNER JOIN LoyaltyPoints lp ON u.Id = lp.CustomerId
GROUP BY u.Id, u.UserName, u.Email
HAVING SUM(lp.Points) > 0
ORDER BY TotalPoints DESC
LIMIT 10;

-- 4. Monthly loyalty points summary
SELECT 
    YEAR(lp.EarnedDate) as Year,
    MONTH(lp.EarnedDate) as Month,
    COUNT(DISTINCT lp.CustomerId) as UniqueCustomers,
    SUM(CASE WHEN lp.Points > 0 THEN lp.Points ELSE 0 END) as TotalEarned,
    SUM(CASE WHEN lp.Points < 0 THEN ABS(lp.Points) ELSE 0 END) as TotalRedeemed,
    SUM(lp.Points) as NetPoints
FROM LoyaltyPoints lp
WHERE lp.EarnedDate >= DATE_SUB(CURDATE(), INTERVAL 12 MONTH)
GROUP BY YEAR(lp.EarnedDate), MONTH(lp.EarnedDate)
ORDER BY Year DESC, Month DESC;

-- 5. Customers with highest redemption rate
SELECT 
    u.UserName,
    u.Email,
    SUM(CASE WHEN lp.Points > 0 THEN lp.Points ELSE 0 END) as EarnedPoints,
    SUM(CASE WHEN lp.Points < 0 THEN ABS(lp.Points) ELSE 0 END) as RedeemedPoints,
    CASE 
        WHEN SUM(CASE WHEN lp.Points > 0 THEN lp.Points ELSE 0 END) > 0 
        THEN (SUM(CASE WHEN lp.Points < 0 THEN ABS(lp.Points) ELSE 0 END) / SUM(CASE WHEN lp.Points > 0 THEN lp.Points ELSE 0 END)) * 100
        ELSE 0
    END as RedemptionRate
FROM AspNetUsers u
INNER JOIN LoyaltyPoints lp ON u.Id = lp.CustomerId
GROUP BY u.Id, u.UserName, u.Email
HAVING EarnedPoints > 0
ORDER BY RedemptionRate DESC;

-- =============================================
-- Sample Insert Statements for Testing
-- =============================================

-- Add sample loyalty points for testing
-- INSERT INTO LoyaltyPoints (Id, CustomerId, Points, EarnedDate, Description)
-- VALUES 
--     (UUID(), 'customer-id-1', 500, NOW(), 'Earned 500 points from ticket purchase (LKR 5000.00)'),
--     (UUID(), 'customer-id-1', -200, NOW(), 'Redeemed 200 points for discount'),
--     (UUID(), 'customer-id-2', 300, NOW(), 'Earned 300 points from ticket purchase (LKR 3000.00)');
